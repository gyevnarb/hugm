import json
import numpy as np
import matplotlib.pyplot as plt
import pandas as pd

dif_type = "map"

# Load the data
graph = json.load(open("data/graph.json"))
dists = json.load(open("data/dists.json"))
map = json.load(open("data/MAP.json"))
expected = json.load(open("data/expected.json"))
stds = json.load(open("data/stds.json"))

alpha = 0.4 * 255
dist_colors = [(100, 255, 200, alpha), (100, 100, 0, alpha), (100, 100, 100, alpha), (0, 100, 0, alpha), (0, 100, 100, alpha), (100, 100, 100, alpha),
               (200, 0, 0, alpha), (200, 200, 0, alpha), (200, 200, 200, alpha), (0, 200, 0, alpha), (0, 200, 200, alpha), (0, 0, 200, alpha),
               (250, 0, 0, alpha), (250, 250, 0, alpha), (250, 250, 250, alpha), (0, 250, 0, alpha), (0, 250, 250, alpha), (0, 0, 250, alpha)]
titles = {"map": "MAP Difference", "expected": "Mean Difference"}

locations = np.array([(n["x"], n["y"]) for n in graph])
gt_districts = np.array([n["district"] for n in graph])
distribution = np.array([dists[i] for i in dists])
map_districts = np.array([map[i] for i in map])
expected_districts = np.array([expected[i] for i in expected])
std_districts = np.array([stds[i] for i in stds])

map_dif = map_districts == gt_districts
expected_dif = np.isclose(expected_districts, gt_districts, atol=std_districts)

if dif_type == "map":
    dif = map_dif
    stats = pd.DataFrame(columns=["District", "Type", "Total Area Count", "Wrong Area Ratio"])
elif dif_type == "expected":
    dif = expected_dif
    stats = pd.DataFrame(columns=["District", "Type", "Total Area Count", "Wrong Area Ratio", "Mean", "Std",
                                  "Max Mean", "Max Std", "Min Mean", "Min Std"])

# Print summary stats


data = pd.DataFrame(columns=["District", "Area", "Marked", "Adjacents", "X", "Y", "Incorrect"])
for i, g in enumerate(graph):
    data = data.append({"District": g["district"],
                        "Area": g["area"],
                        "Marked": g["marked"],
                        "Adjacents": len(g["adjacents"]),
                        "X": g["x"],
                        "Y": g["y"],
                        "Incorrect": not dif[i]}, ignore_index=True)

district_groups = data.groupby("District")
for i in range(18):
    total_area = district_groups.count()["Area"].iloc[i]
    wrong_area = district_groups.sum()["Incorrect"].iloc[i]
    stats = stats.append({"District": data.iloc[i]["District"] + 1,
                          "Type": dif_type,
                          "Total Area Count": total_area,
                          "Wrong Area Ratio": wrong_area / total_area}, ignore_index=True)

stats.to_csv("stats.csv")
print(stats.to_string())

# Plot points
colors = []
for i, n in enumerate(graph):
    colors.append(dist_colors[n["district"] - 1] if dif[i] else (0, 0, 0, 255))
colors = np.array(colors) / 255

plt.figure(titles[dif_type], figsize=(15, 15))
plt.scatter(locations[:, 0], locations[:, 1], c=colors, s=10)
plt.title(titles[dif_type] + "; 1000 runs; 5 steps")
plt.tight_layout()
plt.savefig("map.png")
plt.show()
