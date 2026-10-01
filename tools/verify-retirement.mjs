import assert from 'node:assert/strict';
import worker from '../dist/server/index.js';
import {createEnv} from './local-workspace.mjs';
const env=createEnv();
const call=async(route,method='GET',body)=>{const response=await worker.fetch(new Request('https://example.com'+route,{method,...(body?{headers:{'content-type':'application/json'},body:JSON.stringify(body)}:{})}),env,{});return {status:response.status,data:await response.json()};};
try {
 const old={task:'Old draft',action:'workbench',decision:'Pending review',note:'Local approval id: 27; run: old-run',content:'Old artifact'};
 const inserted=await call('/api/approvals','POST',old);assert.equal(inserted.status,202);
 assert.equal((await call('/api/approvals/reset','POST',{})).status,400);
 const reset=await call('/api/approvals/reset','POST',{confirmation:'retire-all-approval-items',localApprovalMax:27});assert.equal(reset.data.retired,1);
 assert.deepEqual((await call('/api/approvals')).data.approvals,[]);
 assert.equal((await call('/api/approvals','POST',old)).status,409);
 assert.equal((await call('/api/approvals/'+inserted.data.id,'PATCH',{status:'queued',result:'Try to revive'})).status,404);
 assert.equal((await call('/api/approval-detail/'+inserted.data.id)).status,404);
 assert.equal((await call('/api/approvals','POST',{...old,task:'CovePM daily batch',note:'Local approval id: 28; run: new-run'})).status,409);
 const fresh={...old,task:'Averion Compass daily batch',note:'Local approval id: 28; run: new-run'};
 assert.equal((await call('/api/approvals','POST',fresh)).status,202);
 assert.equal((await call('/api/approvals','POST',fresh)).status,409);
 assert.equal((await call('/api/approvals')).data.approvals.length,1);
 console.log('PASS: queue reset, durable retirement, blocked resurrection, legacy brand rejection, fresh Compass work and duplicate import prevention.');
} finally {env.database.close();}
