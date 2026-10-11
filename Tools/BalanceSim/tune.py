import re, sys, subprocess
# tune.py SCALE_GROWTH GATE_GROWTH [gate overrides like 3=8000]
# Rewrites Scale and GateCost in YardDefs.cs:
#   Scale_k = price_k / 5 * SCALE_GROWTH^(k-1)     (tile costs relative to what the yard's product sells for)
#   Gate_k  = base_k * GATE_GROWTH^(k-2)           (base = the hand-picked starting gate prices)
# then runs the console balance simulator (Tools/BalanceSim/console, needs the .NET SDK).
# Current pacing: python tune.py 1.22 p90000x1.2 2=12000
import os
here = os.path.dirname(os.path.abspath(__file__))
path = os.path.join(here, '..', '..', 'Assets', '_Game', 'Scripts', 'Data', 'YardDefs.cs')
prices = {1: 5, 2: 40, 3: 260, 4: 1600, 5: 10000, 6: 62000, 7: 380000, 8: 2.4e6, 9: 1.5e7}
gate_base = {2: 2500, 3: 22000, 4: 180000, 5: 1.4e6, 6: 1.1e7, 7: 8.5e7, 8: 6.5e8, 9: 5e9}
sg = float(sys.argv[1]); gg = 1.0 if sys.argv[2].startswith("p") else float(sys.argv[2])
over = {}
for a in sys.argv[3:]:
    k, v = a.split('='); over[int(k)] = float(v)
s = open(path, encoding='utf-8').read()

def fmt(v):
    if v >= 1e6: return f'{v:.3g}'.replace('e+0', 'e').replace('e+', 'e')
    return str(int(round(v)))

gate_mult = None
if sys.argv[2].startswith('p'):
    # price mode: gate_k = price_(k-1) x M x growth^(k-3)  (p<M>x<growth>)
    m, g = sys.argv[2][1:].split('x'); gate_mult = (float(m), float(g))
for k in range(1, 10):
    scale = prices[k] / 5 * sg ** (k - 1)
    if gate_mult and k > 2:
        gate = prices[k - 1] * gate_mult[0] * gate_mult[1] ** (k - 3)
    else:
        gate = gate_base.get(k, 0) * (1 if gate_mult else gg ** max(0, k - 2))
    gate = over.get(k, gate) if k > 1 else 0
    pat = re.compile(r'(Index = %d, Name = "[^"]+",.*?Scale = )([0-9.e]+)(, GateCost = )([0-9.e]+)' % k, re.S)
    m = pat.search(s)
    assert m, k
    s = s[:m.start(2)] + fmt(scale) + s[m.end(2):m.start(4)] + fmt(gate) + s[m.end(4):]
open(path, 'w', encoding='utf-8').write(s)
for k in range(1, 10):
    m = re.search(r'Index = %d, Name = "([^"]+)",.*?Scale = ([0-9.e]+), GateCost = ([0-9.e]+)' % k, s, re.S)
    print(k, m.group(1), 'scale', m.group(2), 'gate', m.group(3))
out = subprocess.run(['dotnet', 'run', '-c', 'Release'], cwd=os.path.join(here, 'console'), capture_output=True, text=True)
print(out.stdout[-2500:] or out.stderr[-2500:])
