import fs from 'node:fs';
import {generate} from '../src/experiments/tail/generator.js';
import {inspect} from '../src/experiments/tail/checker.js';
const rows=[];
for(const name of ['middle-tail','same-direction-shift','straight-main-left','curved-main-left']){
 const {input}=JSON.parse(fs.readFileSync(`tests/tail-fixtures/${name}.json`));input.obstacles=[];input.joins=[];
 const normal=generate(input).candidates[1];
 for(const fault of ['none','position','join-gap','reverse-tail','fake-tail','short-tail','degenerate','numeric','low-resolution']){
  const output=structuredClone(normal);let resolution={};
  if(fault==='position')output.segments[0].controls[0][0]+=1;
  if(fault==='join-gap')output.segments[1].controls[0][0]+=1;
  if(fault==='reverse-tail')output.segments[1].controls.reverse();
  if(fault==='fake-tail')output.segments[1].kind='bezier';
  if(fault==='short-tail')output.segments[1].controls[0]=output.segments[1].controls[1].map((v,k)=>v-input.end.direction[k]*input.tail/2);
  if(fault==='degenerate')output.segments[0].controls=output.segments[0].controls.map(()=>output.segments[0].controls[0]);
  if(fault==='numeric')output.segments[0].controls[1][0]=1e308;
  if(fault==='low-resolution')resolution={tolerance:1e-9,maxDepth:0};
  const result=inspect(input,output,resolution);
  const category=result.status==='VERIFIED_BOUNDED'?'verified':result.status==='UNKNOWN'?'unknown':'rejected';
  rows.push({name:name+'/'+fault,input,output,resolution,expectedCategory:category,jsResult:result.status,issues:result.issues.map(i=>i.code)});
 }
}
fs.writeFileSync('../../tests/curve-verifier-vectors.json',JSON.stringify({scope:'Flat constant-width local planar checks only, obstacles removed; differential comparison is not independent proof',rows},null,2)+'\n');
