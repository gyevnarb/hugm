import zipfile
with zipfile.ZipFile('2020_1_26_43_50.zip', 'r') as zip_ref:
    zip_ref.extractall('data')
	
from os import walk
import numpy as np

files = []
for (dirpath, dirnames, filenames) in walk('data//2020_1_26_43_50'):
    files.extend(filenames)
    break

data = []

for file in files:
    with open('data//2020_1_26_43_50//' + file, 'r') as f:
        data.append(f.read().split(';'))

print(data)

import matplotlib.pyplot as plt
import numpy as np

dd = np.array([])
dd.resize(19)

for e in data:
  dd[int(e[4])] += 1

print(dd)

plt.bar(range(len(dd)), dd, align='center')
plt.xticks(range(len(dd)), np.arange(19))
plt.show()