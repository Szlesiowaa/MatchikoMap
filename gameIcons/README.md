Lista gier została w 80% wzięta z https://steam250.com/most_played, a w 20% ręcznie dodane tytuły.

Udostępniam te skrypty w celu udokumentowania pracy.

`scriptDownload.py` to skrypt pobrany z https://github.com/SI-Mehdi/steam-game-art-downloader i przerobiony pod moje potrzeby

pozwala na pobranie grafiki gier z `steamgriddb`, do działania potrzebny jest klucz api dołączany zgodnie z instrukcją na powyższym repozytorium


`scriptDelete.py`, `scriptCheck.py` i `scriptRename.py` były pomocne w zmianie nazwy plików i usunięciu niepotrzebnych grafik

`jsonCreator.py` służy do stworzenia pliku `lista_gier.json`, który znajduje się w katalogu Data projektu, plik ten tworzy linki z których zdjęcia będą pobierane

`scriptUpload.py` pozwolił na wysłanie wszystkich tych grafik na zewnętrzny dysk azure blob storage, do działania wymagany jest connectionString