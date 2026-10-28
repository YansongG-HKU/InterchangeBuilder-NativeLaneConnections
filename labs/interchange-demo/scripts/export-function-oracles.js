// Independent analytic polynomials, not calls to the curve generator or inspector.
import {writeFileSync} from 'node:fs';
const rows=Array.from({length:21},(_,i)=>{
  const t=i/20;
  return {t, x:t, y:t*t, curvature:2/Math.pow(1+4*t*t,1.5),
    height:12*(3*t*t-2*t*t*t), grade:.36*t*(1-t)};
});
writeFileSync('../../tests/curve-function-oracles.json',JSON.stringify({scope:'Analytic parabola and 200m/12m zero-end-grade cubic; independently expressed polynomials',rows},null,2)+'\n');
