<p align="center">
  <img src="https://github.com/user-attachments/assets/a440a36b-3456-48e9-9910-1686a6ab4e91">
</p>

<div align="center">
  <a href="README.md">
    <img align="center" src="https://github.com/user-attachments/assets/67f5ef5e-09f2-47a4-a3a4-2fd527d6bd02" width="20">
    English
  </a> &nbsp;|
  <a href="README.ru.md">
    <img align="center" src="https://github.com/user-attachments/assets/bdf8afb3-d027-4a28-8f0c-3ee25fcedd56" width="20">
    Русский
  </a> &nbsp;|
  <a href="README.uk.md">
    <img align="center" src="https://github.com/user-attachments/assets/6734f63d-1d28-46ce-9732-790055d5a54a" width="20">
    Українська
  </a> &nbsp;| 
  <a href="README.zh.md">
    <img align="center" src="https://github.com/user-attachments/assets/86d69702-c489-44c1-902a-520b43a92853" width="20">
    简体中文
  </a> &nbsp;| 
  <a href="README.jp.md">
    <img align="center" src="https://github.com/user-attachments/assets/314ff7c7-4b34-4797-b088-db49ce38a991" width="20">
    日本語
  </a>
</div>

## About the Program
**Minecraft Localizer** is a tool for localizing Minecraft mods.  
The application allows automatic translation of mods **for free** using [GPT4Free](https://github.com/xtekky/gpt4free/) or **with higher translation quality** via [DeepSeek API](https://www.deepseek.com/) (paid mode), as well as managing translation, saving results, and tracking operation status through a convenient GUI.

## Features
- **Automatic Translation**  
  Supports translation to any game language using:
  - [GPT4Free](https://github.com/xtekky/gpt4free/) — no API key required; limitations depend on the provider;
  - [DeepSeek API](https://www.deepseek.com/) — requires API key for maximum accuracy and stability.

- **Multiple Modes**: Ability to choose different translation modes (*Single File, Mods, Resource Pack, FTB Quests, Patchouli, Better Questing*).

- **Flexible GPT4Free Settings**: Select provider and model, plus additional parameters (temperature, batch size).

- **Flexible DeepSeek Settings**: Quality/speed parameters and API key usage.

- **Convenient Saving**: After translation, results are automatically saved; manual saving is also possible.

- **Interactive Interface**: Manage translation via a user-friendly interface.

- **Settings and Directories**: Easy settings management and quick access to directories.

- **Logs and Status**: Separate window showing GPT4Free installation/start progress and logs.

## Usage
> [!IMPORTANT]
> To use [GPT4Free](https://github.com/xtekky/gpt4free/), install:
>
>| Component | Required Version | Download |
>|------------|-----------------|---------|
>| Python     | 3.10 or higher  | www.python.org |
>| Git        | Any latest      | www.git-scm.com |

1. **Go to Settings** and select the desired language and game directory, then save changes.
2. **Select Translation Mode** in the main window.
3. **Load Data**:
   - for *Mods / FTB Quests / Patchouli / Better Questing* — select the desired mod from the list;
   - for *Single File* or *Resource Pack* — use **File → Open File** or **File → Open Resource Pack**.
4. **Click "Start Translation"**.
   - For **GPT4Free**, a window for installation/start will open.
   - For **DeepSeek**, just enter the API key in settings.
5. **Wait for Translation Completion**. You can cancel and save already translated strings if necessary.
6. **Check the "resourcepacks" folder** in the game directory or the save path from the notification. If no result, click **"Save Translation"**.

> [!WARNING]
> In some regions (e.g., Russia), access to most GPT4Free providers may be restricted. Use a VPN for stable operation.  
> Availability and limits may also depend on the provider.

> [!TIP]
> If a mod or file is read incorrectly, switch reading mode from **"Table"** to **"Text"**.  
> In this mode, the AI translates the entire content, preserving all special characters, formatting, and file structure instead of selectively processing elements.

> [!IMPORTANT]
> For the best translation quality, use **DeepSeek**.  
> It delivers significantly more accurate meaning, correctly accounts for context, and rarely makes mistakes.  
> Achieving the same stability with GPT4Free is usually not possible.

## Contacts
If you have questions or suggestions, create an Issue on GitHub or contact me via [Telegram](https://t.me/Alex_Serbet).

## License
This project is licensed under the MIT License.