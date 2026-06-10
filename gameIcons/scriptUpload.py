import os
import json
import urllib.parse
from azure.storage.blob import BlobServiceClient

# CONFIG
CONNECTION_STRING = ""
CONTAINER_NAME = "game-art"
JSON_FILE = "games.json"
BASE_DIR = "./games"

blob_service = BlobServiceClient.from_connection_string(CONNECTION_STRING)
container = blob_service.get_container_client(CONTAINER_NAME)

containers = blob_service.list_containers()
print("Dostępne kontenery:")
for c in containers:
    print(f"- {c.name}")

def get_blob_path_from_url(url):
    if not url:
        return None

    parts = url.split(f"{CONTAINER_NAME}/")
    if len(parts) < 2:
        return None

    return urllib.parse.unquote(parts[1])


def find_local_file(folder_path, target):
    for file in os.listdir(folder_path):
        name, ext = os.path.splitext(file)
        if name.lower() == target:
            return os.path.join(folder_path, file)
    return None


def main():
    # JSON (dla URL-i)
    with open(JSON_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    # lista gier (dla sortowania)
    game_names = [g["name"] for g in data]

    # katalogi
    folders = [
        f for f in os.listdir(BASE_DIR)
        if os.path.isdir(os.path.join(BASE_DIR, f))
    ]

    game_names.sort(key=lambda x: x.lower())
    folders.sort(key=lambda x: x.lower())

    if len(game_names) != len(folders):
        print("⚠️ UWAGA: liczba gier i folderów się nie zgadza")

    for i in range(min(len(game_names), len(folders))):
        game_name = game_names[i]
        folder = folders[i]

        folder_path = os.path.join(BASE_DIR, folder)

        print(f"\n== {game_name} → {folder} ==")

        for target in ["icon", "grid", "poster"]:
            local_file = find_local_file(folder_path, target)

            if not local_file:
                print(f"Brak: {target}")
                continue

            name, ext = os.path.splitext(local_file)
            file_name = os.path.basename(local_file)

            # 🔑 budujemy ścieżkę dokładnie jak w JSON:
            # games/{folder}/{file}
            blob_path = f"games/{folder}/{file_name}"
            print(blob_path)

            with open(local_file, "rb") as data_stream:
                container.upload_blob(
                    name=blob_path,
                    data=data_stream,
                    overwrite=True
                )

            print(f"✔ Uploaded: {blob_path}")


if __name__ == "__main__":
    main()
