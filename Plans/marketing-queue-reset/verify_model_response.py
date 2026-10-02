from pathlib import Path
import sys, io, json
from unittest.mock import patch
sys.path.insert(0,str(Path('../averion-software/products/cove-pm/marketing-vault/agent-workbench').resolve()))
import run

valid={'brief':'A field note','draft':'Averion Compass. Talk with us.','claims':[{'claim':'Example','source':'none','status':'supported'}],'questions':[],'approval_required':True}
def response(value):
    return io.BytesIO(json.dumps({'message':{'content':json.dumps(value)}}).encode())
with patch('urllib.request.urlopen',side_effect=[response({}),response(valid)]) as calls:
    assert run.call_ollama('http://example.com','fixture-model','prompt') == valid
    assert calls.call_count == 2
    request_body=json.loads(calls.call_args[0][0].data)
    assert 'draft' in request_body['format']['required']
with patch('urllib.request.urlopen',side_effect=[response({}),response({})]):
    try:
        run.call_ollama('http://example.com','fixture-model','prompt')
        raise AssertionError('Empty result incorrectly accepted')
    except RuntimeError as e:
        assert 'empty or incomplete' in str(e)
print('PASS: structured draft contract, retry on empty model output, and explicit failure after empty retry.')
