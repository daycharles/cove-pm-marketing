"""LangGraph orchestration for the minimal local marketing workflow.

The graph deliberately keeps the same small contract as run.py:
prepare -> generate -> qa -> persist.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any, TypedDict

from langgraph.checkpoint.memory import MemorySaver
from langgraph.graph import END, START, StateGraph

import run as workbench


class MarketingState(TypedDict, total=False):
    task_path: str
    config_path: str
    mock: bool
    run_id: str
    started_at: str
    model: str
    task: workbench.Task
    config: dict[str, Any]
    context: list[tuple[str, str]]
    result: dict[str, Any]
    qa: list[str]
    output_path: str
    status: str
    error: str


def prepare_node(state: MarketingState) -> MarketingState:
    task_path = Path(state["task_path"]).resolve()
    config_path = Path(state["config_path"]).resolve()
    config = workbench.load_config(config_path)
    task = workbench.load_task(task_path)
    return {
        "task": task,
        "config": config,
        "context": workbench.load_context(config),
        "run_id": workbench.datetime.now(workbench.timezone.utc).strftime("%Y%m%dT%H%M%SZ"),
        "started_at": workbench.now_iso(),
        "model": str(config.get("model", "qwen3:4b")),
    }


def generate_node(state: MarketingState) -> MarketingState:
    task = state["task"]
    if state.get("mock", False):
        result = workbench.mock_response(task)
    else:
        prompt = workbench.make_prompt(task, state.get("context", []))
        result = workbench.call_ollama(str(state["config"]["ollama_url"]), state["model"], prompt)
    return {"result": result}


def qa_node(state: MarketingState) -> MarketingState:
    qa = workbench.run_qa(state["result"], state["task"])
    return {"qa": qa, "status": "needs-review" if qa else "ready-for-human-review"}


def persist_node(state: MarketingState) -> MarketingState:
    config = state["config"]
    output_dir = workbench.resolve_config_path(str(config.get("output_dir", "outputs")))
    db_path = workbench.resolve_config_path(str(config.get("data_dir", "data"))) / "runs.sqlite3"
    workbench.init_db(db_path)
    output_dir.mkdir(parents=True, exist_ok=True)
    output_path = output_dir / f"{state['run_id']}-{workbench.slugify(state['task'].objective)}.md"
    output_path.write_text(workbench.render_output(state["task"], state["result"], state.get("qa", []), state["run_id"], state["model"]), encoding="utf-8")
    workbench.save_run(db_path, (state["run_id"], state["started_at"], workbench.now_iso(), str(state["task"].path), state["model"], state["status"], str(output_path), json.dumps(state.get("qa", [])), None))
    return {"output_path": str(output_path)}


def build_graph():
    graph = StateGraph(MarketingState)
    graph.add_node("prepare", prepare_node)
    graph.add_node("generate", generate_node)
    graph.add_node("qa", qa_node)
    graph.add_node("persist", persist_node)
    graph.add_edge(START, "prepare")
    graph.add_edge("prepare", "generate")
    graph.add_edge("generate", "qa")
    graph.add_edge("qa", "persist")
    graph.add_edge("persist", END)
    return graph.compile(checkpointer=MemorySaver())


def main() -> int:
    parser = argparse.ArgumentParser(description="Run the CovePM marketing workflow through LangGraph")
    parser.add_argument("--task", type=Path, required=True)
    parser.add_argument("--config", type=Path, default=workbench.DEFAULT_CONFIG)
    parser.add_argument("--mock", action="store_true")
    args = parser.parse_args()
    task_path = args.task if args.task.is_absolute() else (workbench.ROOT / args.task).resolve()
    config_path = args.config if args.config.is_absolute() else (workbench.ROOT / args.config).resolve()
    graph = build_graph()
    state = graph.invoke({"task_path": str(task_path), "config_path": str(config_path), "mock": args.mock}, {"configurable": {"thread_id": "local-marketing-run"}})
    print(json.dumps({"run_id": state["run_id"], "status": state["status"], "output": state["output_path"], "qa": state.get("qa", [])}, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
