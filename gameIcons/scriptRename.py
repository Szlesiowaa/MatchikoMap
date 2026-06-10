import os

BASE_DIR = "./games"

def rename_posters():
    for game_folder in os.listdir(BASE_DIR):
        game_path = os.path.join(BASE_DIR, game_folder)

        if not os.path.isdir(game_path):
            continue

        files = os.listdir(game_path)

        poster_files = [f for f in files if "icon" in f.lower()]

        if not poster_files:
            print(f"Brak poster w: {game_folder} → pomijam")
            continue

        for file in poster_files:
            old_path = os.path.join(game_path, file)

            name, ext = os.path.splitext(file)
            new_name = f"icon{ext}"
            new_path = os.path.join(game_path, new_name)

            # 🔑 KLUCZOWE: jeśli to ten sam plik → pomiń
            if os.path.abspath(old_path) == os.path.abspath(new_path):
                print(f"Skip (już poprawna nazwa): {old_path}")
                continue

            # jeśli istnieje inny plik o tej nazwie → usuń
            if os.path.exists(new_path):
                os.remove(new_path)

            os.rename(old_path, new_path)
            print(f"{old_path} → {new_path}")

if __name__ == "__main__":
    rename_posters()
