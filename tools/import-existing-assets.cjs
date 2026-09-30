const fs=require('node:fs');
const path=require('node:path');
const source=process.argv[2];
if(!source||!path.isAbsolute(source))throw new Error('Supply the absolute path of the verified production dist directory.');
const destination=path.resolve(__dirname,'../dist');
let copied=0;
function copyMissing(from,to){for(const entry of fs.readdirSync(from,{withFileTypes:true})){const src=path.join(from,entry.name),dst=path.join(to,entry.name);if(entry.isSymbolicLink())throw new Error('Unexpected symbolic link');if(entry.isDirectory()){fs.mkdirSync(dst,{recursive:true});copyMissing(src,dst);}else if(!fs.existsSync(dst)){fs.copyFileSync(src,dst);copied++;}}}
copyMissing(source,destination);
console.log(`Imported ${copied} unchanged production assets without replacing upgraded files.`);
