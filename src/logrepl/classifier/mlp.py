"""Багатошаровий перцептрон, написаний самостійно."""


class MLP:
    def fit(self, x, y) -> None:
        raise NotImplementedError

    def predict(self, x) -> list[int]:
        raise NotImplementedError
