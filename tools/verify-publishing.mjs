import assert from 'node:assert/strict';
import {publishingCalendar} from '../dist/server/publishing-calendar.js';
const request=new Request('https://example.com/api/publishing-calendar?from=2026-10-01T00:00:00Z&to=2026-11-10T00:00:00Z');
const env={PUBLORA_API_KEY:'fixture-secret'};
assert.equal((await publishingCalendar(request,{})).status,503);
assert.equal((await publishingCalendar(new Request('https://example.com/api/publishing-calendar?from=bad&to=bad'),env)).status,400);
const calls=[];
const fetcher=async(url,options)=>{
  url=new URL(url);calls.push(url.pathname);assert.equal(options.headers['x-publora-key'],'fixture-secret');
  if(url.pathname.endsWith('platform-connections'))return Response.json({connections:[{platformId:'linkedin-123',username:'Averion Software'}]});
  assert.equal(url.searchParams.get('fromDate'),'2026-10-01T00:00:00.000Z');
  return Response.json({posts:[{postGroupId:'post-'+url.searchParams.get('page'),content:'Sample post',status:'scheduled',scheduledTime:'2026-10-02T00:30:00Z',platforms:[{platformId:'linkedin-123',platform:'linkedin',status:'scheduled'}],mediaUrls:['javascript:alert(1)','https://example.com/image.png']}],pagination:{hasNextPage:url.searchParams.get('page')==='1'}});
};
const result=await publishingCalendar(request,env,fetcher);
assert.equal(result.status,200);assert.equal(result.body.posts.length,2);assert.equal(result.body.posts[0].accounts[0].name,'Averion Software');assert.deepEqual(result.body.posts[0].mediaUrls,['https://example.com/image.png']);
assert.equal(JSON.stringify(result).includes('fixture-secret'),false);
assert.equal((await publishingCalendar(request,env,async()=>{throw new Error('fixture-secret')})).status,502);
assert.equal(new Intl.DateTimeFormat('en-CA',{timeZone:'America/New_York'}).format(new Date('2026-10-02T00:30:00Z')),'2026-10-01');
console.log('PASS: publishing calendar range validation, missing configuration, pagination, account labels, media filtering, Eastern date boundary, and provider failure.');
