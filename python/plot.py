import pandas as pd
import seaborn as sns
import matplotlib.pyplot as plt

df = pd.read_csv("generated.stat", delimiter=";")
idx = list(range(10, 118, 6))
df = df.iloc[:, idx]
df = df.transpose()
df = df.reindex(df.mean().sort_values().index, axis=1)

df.reset_index().plot(kind="box", x="index")
plt.xticks([])

plt.show()
