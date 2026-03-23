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

## 关于程序
**Minecraft Localizer** 是一个用于本地化 Minecraft 模组的工具。  
该应用可以使用 [GPT4Free](https://github.com/xtekky/gpt4free/) **免费自动翻译模组**，或者通过 [DeepSeek API](https://www.deepseek.com/) **获得更高质量的翻译**（付费模式），并可通过 GUI 管理翻译过程、保存结果和跟踪操作状态。

## 功能
- **自动翻译**  
  支持翻译成游戏的任意语言：
  - [GPT4Free](https://github.com/xtekky/gpt4free/) — 不需要 API 密钥；限制取决于提供商
  - [DeepSeek API](https://www.deepseek.com/) — 需要 API 密钥以获得最高精度和稳定性

- **多种模式**: 可选择翻译模式 (*单文件, 模组, Resource Pack, FTB Quests, Patchouli, Better Questing*)

- **灵活的 GPT4Free 设置**: 提供商和模型选择，额外参数（温度、批量大小）

- **灵活的 DeepSeek 设置**: 质量/速度参数及 API 密钥使用

- **方便保存**: 翻译结果自动保存，可手动保存

- **交互式 GUI**: 直观管理翻译

- **设置和目录**: 简单管理设置及快速访问目录

- **日志与状态**: 独立窗口显示 GPT4Free 安装/启动进度及日志

## 使用方法
> [!IMPORTANT]
> 使用 [GPT4Free](https://github.com/xtekky/gpt4free/) 需要安装：
>
>| 组件 | 版本要求 | 下载 |
>|------|---------|-----|
>| Python | 3.10 或更高 | www.python.org |
>| Git    | 任意最新版 | www.git-scm.com |

1. **设置**: 选择语言和游戏目录，保存  
2. **选择翻译模式**  
3. **加载数据**:
   - *Mods / FTB Quests / Patchouli / Better Questing* — 从列表中选择模组  
   - *单文件* 或 *Resource Pack* — **文件 → 打开文件** 或 **文件 → 打开资源包**  
4. **点击 "开始翻译"**  
   - **GPT4Free** — 打开安装/启动窗口  
   - **DeepSeek** — 在设置中输入 API 密钥  
5. **等待翻译完成**。可取消并保存已翻译内容  
6. **检查 "resourcepacks" 文件夹** 或通知中的保存路径。若无结果，点击 **"保存翻译"**

> [!WARNING]
> 某些地区（如俄罗斯）可能限制访问 GPT4Free。建议使用 VPN。

> [!TIP]
> 如果模组或文件读取不正确，请将读取模式从 **“表格”** 切换为 **“文本”**。  
> AI 会完整翻译内容，保留特殊字符、格式和文件结构。

> [!IMPORTANT]
> 为获得最高翻译质量，建议使用 **DeepSeek**。  
> 翻译更准确，考虑上下文，几乎不出错。

## 联系方式
GitHub Issue 或 [Telegram](https://t.me/Alex_Serbet)

## ライセンス
このプロジェクトはMITライセンスの下で配布されています。