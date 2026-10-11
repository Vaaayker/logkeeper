"""Мінімальна перевірка: усі модулі імпортуються, pytest працює з першого коміту."""


def test_modules_import():
    import logrepl.classifier  # noqa: F401
    import logrepl.compression  # noqa: F401
    import logrepl.integration  # noqa: F401
    import logrepl.scheduler  # noqa: F401
    import logrepl.security  # noqa: F401
