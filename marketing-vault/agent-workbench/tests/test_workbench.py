import json
import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import run as workbench


class WorkbenchTests(unittest.TestCase):
    def test_mock_result_passes_required_cta_and_claims(self):
        task = workbench.Task(Path("task.md"), "Draft a page", "Operators", True, "")
        result = workbench.mock_response(task)
        self.assertEqual(workbench.run_qa(result, task), [])

    def test_gated_language_is_flagged(self):
        task = workbench.Task(Path("task.md"), "Draft a page", "Operators", True, "")
        result = workbench.mock_response(task)
        result["draft"] += " Includes a guaranteed discount."
        qa = workbench.run_qa(result, task)
        self.assertTrue(any("guaranteed" in item for item in qa))
        self.assertTrue(any("discount" in item for item in qa))

    def test_mock_run_writes_artifact_and_sqlite(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            task_path = base / "task.md"
            task_path.write_text("---\nobjective: Test draft\naudience: Operators\n---\nDraft it.", encoding="utf-8")
            config_path = base / "config.json"
            config_path.write_text(json.dumps({"model": "test", "output_dir": "outputs", "data_dir": "data", "context_files": []}), encoding="utf-8")
            old_root = workbench.ROOT
            workbench.ROOT = base
            try:
                original_argv = sys.argv
                sys.argv = ["run.py", "--task", str(task_path), "--config", str(config_path), "--mock"]
                self.assertEqual(workbench.main(), 0)
                self.assertTrue((base / "outputs").glob("*.md"))
                self.assertTrue((base / "data" / "runs.sqlite3").exists())
            finally:
                sys.argv = original_argv
                workbench.ROOT = old_root


if __name__ == "__main__":
    unittest.main()
