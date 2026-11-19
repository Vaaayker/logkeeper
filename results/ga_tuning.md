# Підбір параметрів системи ГА (fitness = середній Q за node-loss, two-node-loss, mixed)

| method | evaluations | best_Q | ValueNormal | ValueAnomaly | ValuePreIncident | CriticalBonus | BudgetFactor |
|---|---|---|---|---|---|---|---|
| параметри за замовчуванням | 1 | 0.7780 | 1 | 4 | 10 | 10 | 1.5000 |
| ГА | 92 | 0.8010 | 2.8744 | 1.8405 | 6.5054 | 11.4591 | 2.4949 |
| випадковий пошук | 92 | 0.8011 | 1.1759 | 1.0381 | 2.1636 | 11.3181 | 2.4729 |

- Сесія: перші 300 подій (synthetic(n=300, seed=33)); ГА: популяція 12, 8 поколінь, seed 1.
- Найкращі параметри ГА збережено в results/ga_best_params.csv — їх використовують ablation і final.
