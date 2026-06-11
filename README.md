# MatchikoMap
[Link do strony](https://matchikomap.azurewebsites.net)
## Czym jest MatchikoMap?

MatchikoMap to mały serwis społecznościowy dla graczy, który stara się rozwiązać problem braku prawdziwego kontaktu z drugim człowiekiem z użyciem mapy i lokalizacji.

Oferuje on czaty prywatne, globalne, system matchmakingu oraz funkcję wyszukiwania innych osób w okolicy za pomocą filtrowania lub oceny podobieństw profili.

- Czaty globalne pomagają w tworzeniu lokalnej społeczności wokół danego tytułu gry.
- Matchmaking pozwala na znalezienie kompana do gry "tu i teraz".
- Filtry i ocena podobieństw profili pozwala użytkownikowi na znalezienie dokładnie takiego znajomego, jakiego chce

## Jak używać?
1. Użytkownik tworzy profil. Do wyboru ma rejestrację mailową lub kontem Googla.
2. Po wejściu na stronę główną użytkownik jest proszony o udostępnienie lokalizacji. Bez niej duża część funkcjonalności serwisu nie będzie działać.
3. Użytkownik może zaprosić inną osobę do znajomych podając jej nick. Po zaakceptowaniu przez obie strony znajomy wyświetli się na liście znajomych oraz na mapie.
4. Klikając na znajomego, otwiera się czat, który pozwala na pisanie wiadomości tekstowych oraz wysyłanie zdjęć i filmów.
5. Użytkownik może również edytować swój profil, zdjęcie profilowe oraz swoje preferencje. Serwis umożliwia całkiem szeroką personalizację preferencji.
6. Użytkownik może wyszukać innych użytkowników w pobliżu korzystając z opcji `szukaj znajomych`.
7. Wyszukiwarka służy do dodawania tytułów gier do ulubionych, wyświetlania konwersacji globalnych dedykowanych danej grze w danej okolicy oraz stworzenia lub dołączenia do istniejącego zgłoszenia (które jest ważne przez godzinę). Dzięki temu użytkownik jest w stanie w bardzo wygodny i szybki sposób znaleźć drugą osobę do pogrania w daną grę.

## Stack technologiczny
**Backend**

- C# + .NET 8.0 + ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Identity
- Google OAuth 2.0
- SignalR
- SMTP Gmail

**Frontend**

- HTML
- CSS
- JavaScript
- Leaflet.js

**Deployment**

- Azure App Service
- Azure Flexible PostgreSQL Server
- Azure Blob Storage

