# Releasing

The repository includes a GitHub Actions workflow that builds the WPF app, creates a version tag, packs a `win-x64` release archive, and publishes a GitHub Release automatically.

## Recommended flow
1. Push the branch with the release-ready code to `master`.
2. Open `Actions` on GitHub.
3. Run the `Release` workflow manually.
4. Enter the version as `1.4.0` or `v1.4.0`.
5. Enable `prerelease` only if needed.

## Release notes
- The workflow uses GitHub automatic release notes.
- If you want a custom description, edit the release on GitHub after the workflow finishes.

## Release artifact
- File name: `MinecraftLocalizer-vX.Y.Z-win-x64.zip`
- Contents: published application files, `LICENSE.txt`, and `README.md`

## Notes
- The workflow requires the default `GITHUB_TOKEN` with `contents: write`.
- The tag is created automatically if it does not already exist on `origin`.
