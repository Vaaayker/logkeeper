"""Ознаки з вікном історії (частота подій, зміна metric_value, нестабільність category тощо)."""


def build_features(window: list) -> list[float]:
    raise NotImplementedError
