# Класифікатор подій: якість на valid

| model | accuracy | macro_F1 | F1_class0 | F1_class1 | F1_class2 | train_ms |
|---|---|---|---|---|---|---|
| MLP (перцептрон) | 0.8627 | 0.7675 | 0.9123 | 0.6331 | 0.7571 | 1694.12 |
| правило severity/critical | 0.8780 | 0.7279 | 0.9457 | 0.5212 | 0.7170 | 0 |

- train: synthetic(n=3000, seed=11) (3000); valid: synthetic(n=1500, seed=22) (1500).
