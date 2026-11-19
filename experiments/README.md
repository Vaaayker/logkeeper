# Експерименти

Консольний проєкт `experiments/LogKeeper.Experiments`. Кожен експеримент — окрема команда,
таблиці пишуться в `results/` одночасно у форматах `.csv` і `.md`.

```bash
dotnet run --project experiments/LogKeeper.Experiments -c Release -- <назва>
dotnet run --project experiments/LogKeeper.Experiments -c Release -- all   # усе по черзі
```

| Назва | Що вимірює | Файли в `results/` |
|---|---|---|
| `dp-vs-greedy` | сумарна корисність і час ДП проти жадібного; частка випадкових задач, де жадібний гірший | `dp_vs_greedy.*` |
| `classifier` | accuracy, macro-F1, F1 за класами, матриця помилок на valid; порівняння з правилом | `classifier.*`, `classifier_confusion.*` |
| `ga` | збіжність ГА проти випадкового пошуку (Растригін); підбір параметрів системи з fitness = Q | `ga_convergence.*`, `ga_tuning.*`, `ga_tuning_convergence.*`, `ga_best_params.csv` |
| `ablation` | S, R, T, Q без перцептрона / без ДП / без ГА | `ablation.*` |
| `final` | фінальні S, R, T, Q за всіма сценаріями відмов | `final.*` |

`ablation` і `final` використовують параметри з `results/ga_best_params.csv`, тож `ga` треба запустити раніше
(`all` робить це автоматично). Усі генератори мають фіксовані seed — повторний запуск дає ті самі таблиці
(крім стовпців часу).

Якщо в `materials/` немає `train.csv` / `valid.csv` / `session.bin`, використовується синтетика
(`SyntheticLog`, D-008); джерело даних указано в примітках під кожною таблицею.
