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

## プログラムについて
**Minecraft Localizer** は、MinecraftのMODをローカライズするツールです。  
このアプリは、[GPT4Free](https://github.com/xtekky/gpt4free/) を使った **無料自動翻訳**、または [DeepSeek API](https://www.deepseek.com/) を使った **高品質翻訳（有料）** を提供し、翻訳プロセスの管理、結果保存、操作状況の追跡をGUIで行えます。

## 特徴
- **自動翻訳**  
  ゲームの任意の言語に対応：
  - [GPT4Free](https://github.com/xtekky/gpt4free/) — APIキー不要、プロバイダーによって制限あり  
  - [DeepSeek API](https://www.deepseek.com/) — APIキー必要、高精度で安定した翻訳

- **複数モード対応**: 翻訳モード選択可能 (*単一ファイル, Mods, Resource Pack, FTB Quests, Patchouli, Better Questing*)

- **柔軟なGPT4Free設定**: プロバイダーやモデルの選択、追加パラメータ（温度、バッチサイズ）

- **柔軟なDeepSeek設定**: 品質/速度パラメータ、APIキー使用

- **便利な保存機能**: 翻訳結果は自動保存、手動保存も可能

- **インタラクティブGUI**: 直感的に翻訳管理可能

- **設定とディレクトリ**: 設定管理とディレクトリへの簡単アクセス

- **ログとステータス**: GPT4Freeインストール/起動進捗とログを表示

## 使用方法
> [!IMPORTANT]
> [GPT4Free](https://github.com/xtekky/gpt4free/) を使用するには、以下をインストール：
>
>| コンポーネント | 必須バージョン | ダウンロード |
>|---------------|----------------|-------------|
>| Python        | 3.10以上       | www.python.org |
>| Git           | 最新版         | www.git-scm.com |

1. **設定**で言語とゲームディレクトリを選択して保存。  
2. **翻訳モード選択**。  
3. **データ読み込み**:
   - *Mods / FTB Quests / Patchouli / Better Questing* — 左リストからMODを選択  
   - *単一ファイル* または *Resource Pack* — **ファイル → ファイルを開く** または **ファイル → リソースパックを開く**  
4. **「翻訳開始」ボタンをクリック**  
   - **GPT4Free** ではインストール/起動ウィンドウが表示  
   - **DeepSeek** では設定でAPIキー入力  
5. **翻訳完了まで待機**。必要に応じてキャンセル可能  
6. **"resourcepacks" フォルダ** または通知の保存先を確認。結果がない場合は **「翻訳を保存」** をクリック

> [!WARNING]
> 一部地域（例：ロシア）ではGPT4Freeへのアクセスが制限される場合があります。安定動作にはVPN推奨。

> [!TIP]
> MODやファイルが正しく読み込めない場合、読み取りモードを **「テーブル」** から **「テキスト」** に切替。  
> このモードではAIが全内容を翻訳し、特殊文字やフォーマット、ファイル構造を保持します。

> [!IMPORTANT]
> 最高品質の翻訳には **DeepSeek** 推奨。  
> 文脈を正しく理解し、誤りがほとんどない高精度翻訳を提供。  
> GPT4Freeで同等の安定性は通常達成できません。

## 連絡先
質問や提案がある場合、GitHub Issueまたは [Telegram](https://t.me/Alex_Serbet) で連絡してください。

## 许可证
本项目使用MIT许可证进行分发。