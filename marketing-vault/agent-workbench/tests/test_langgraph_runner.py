import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import langgraph_runner
import run as workbench


class LangGraphRunnerTests(unittest.TestCase):
    def test_graph_runs_minimal_workflow_in_mock_mode(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            task_path = base / "task.md"
            task_path.write_text("---\nobjective: Graph test\naudience: Operators\n---\nDraft it.", encoding="utf-8")
            config_path = base / "config.json"
            config_path.write_text(json.dumps({"model": "test", "output_dir": "outputs", "data_dir": "data", "context_files": []}), encoding="utf-8")
            old_root = workbench.ROOT
            workbench.ROOT = base
            try:
                graph = langgraph_runner.build_graph()
                state = graph.invoke({"task_path": str(task_path), "config_path": str(config_path), "mock": True}, {"configurable": {"thread_id": "test-thread"}})
                self.assertEqual(state["status"], "ready-for-human-review")
                self.assertTrue(Path(state["output_path"]).exists())
                self.assertTrue((base / "data" / "runs.sqlite3").exists())
            finally:
                workbench.ROOT = old_root


if __name__ == "__main__":
    unittest.main()
