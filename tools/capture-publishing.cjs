const {chromium}=require('C:/Users/cd104535/AppData/Local/npm-cache/_npx/e41f203b7505f1fb/node_modules/playwright');
const {spawn}=require('node:child_process');const fs=require('node:fs');const path=require('node:path');
(async()=>{
 const root=path.resolve(__dirname,'..');const evidence=path.join(root,'.review/publishing-calendar');fs.mkdirSync(evidence,{recursive:true});
 const child=spawn(process.execPath,['tools/preview-workspace.mjs'],{cwd:root,windowsHide:true,stdio:['ignore','pipe','pipe']});let browser;
 try{
  await new Promise((resolve,reject)=>{child.stdout.once('data',resolve);child.once('exit',code=>reject(new Error('Preview exited '+code)));});
  browser=await chromium.launch({executablePath:'C:/Users/cd104535/AppData/Local/ms-playwright/chromium-1208/chrome-win64/chrome.exe',headless:true});
  const context=await browser.newContext({viewport:{width:1440,height:960},recordVideo:{dir:evidence,size:{width:1440,height:960}}});const page=await context.newPage();let mode='sample';
  const now=new Date();const day=new Intl.DateTimeFormat('en-CA',{timeZone:'America/New_York',year:'numeric',month:'2-digit',day:'2-digit'}).format(now);
  const posts=[{id:'local-preview-post',content:'LOCAL PREVIEW ONLY: a sample inspection field note with a new social asset.',status:'scheduled',scheduledTime:day+'T18:30:00Z',accounts:[{name:'Averion Software · SAMPLE',platform:'linkedin',status:'scheduled'}],mediaUrls:[]}];
  await page.route('**/api/publishing-calendar?*',route=>mode==='failure'?route.fulfill({status:502,json:{error:'Sample provider outage'}}):route.fulfill({json:{posts:mode==='empty'?[]:posts,timeZone:'America/New_York'}}));
  await page.goto('http://127.0.0.1:8897/#calendar');await page.getByText('Work and publishing, together',{exact:true}).waitFor();await page.locator('[data-post]').first().waitFor();
  await page.screenshot({path:path.join(evidence,'calendar.png'),fullPage:true});await page.waitForTimeout(1200);
  await page.locator('[data-post]').first().click();await page.getByText('This calendar is read-only.',{exact:false}).waitFor();await page.waitForTimeout(1600);await page.getByRole('button',{name:'Close details'}).click();
  await page.getByRole('button',{name:'Next month'}).click();await page.waitForTimeout(1200);await page.locator('[data-month="0"]').click();
  mode='failure';await page.locator('#refresh').click();await page.getByText('Publora refresh failed.',{exact:false}).waitFor();if(!await page.locator('[data-post]').count())throw new Error('Stale posts disappeared');await page.waitForTimeout(1200);
  mode='empty';await page.locator('#refresh').click();await page.getByText('No scheduled posts in this range',{exact:true}).waitFor();await page.waitForTimeout(1200);
  const video=page.video();await context.close();fs.renameSync(await video.path(),path.join(evidence,'publishing-calendar.webm'));
  const mobile=await browser.newContext({viewport:{width:390,height:844}});const mp=await mobile.newPage();await mp.route('**/api/publishing-calendar?*',route=>route.fulfill({json:{posts,timeZone:'America/New_York'}}));await mp.goto('http://127.0.0.1:8897/#calendar');await mp.locator('[data-post]').first().waitFor();await mp.screenshot({path:path.join(evidence,'mobile.png'),fullPage:true});if(await mp.evaluate(()=>document.documentElement.scrollWidth>innerWidth)){console.log(await mp.evaluate(()=>[...document.querySelectorAll('*')].filter(x=>x.getBoundingClientRect().right>innerWidth+1).map(x=>({tag:x.tagName,class:x.className,right:x.getBoundingClientRect().right})).slice(0,15)));throw new Error('Mobile overflow');}await mobile.close();console.log('PASS: calendar post detail, month navigation, stale feed retention, empty state, mobile layout; review clip captured.');
 }finally{if(browser)await browser.close();child.kill();}
})().catch(e=>{console.error(e);process.exitCode=1});
