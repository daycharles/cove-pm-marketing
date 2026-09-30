import { DatabaseSync } from 'node:sqlite';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
export function createEnv(filename=':memory:') {
  const database = new DatabaseSync(filename);
  const prepare = (sql) => {
    let values=[];
    const query={bind(...args){values=args;return query;},async all(){return {results:database.prepare(sql).all(...values)};},async first(){return database.prepare(sql).get(...values)||null;},async run(){const r=database.prepare(sql).run(...values);return {meta:{changes:Number(r.changes),last_row_id:Number(r.lastInsertRowid)}};}};
    return query;
  };
  const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../dist/client');
  return { database, DB:{prepare,async batch(queries){database.exec('BEGIN');try{const rows=[];for(const q of queries)rows.push(await q.run());database.exec('COMMIT');return rows;}catch(e){database.exec('ROLLBACK');throw e;}}},ASSETS:{async fetch(request){let name=decodeURIComponent(new URL(request.url).pathname);if(name==='/')name='/index.html';const file=path.resolve(root,'.'+name);if(!file.startsWith(root+path.sep))return new Response('Not found',{status:404});try{const types={'.html':'text/html','.js':'text/javascript','.css':'text/css','.png':'image/png','.md':'text/markdown'};return new Response(await readFile(file),{headers:{'content-type':types[path.extname(file)]||'application/octet-stream'}});}catch{return new Response('Not found',{status:404});}}}};
}
