# MultibootOS official templates

The distribution templates the MultibootOS Image Builder shows as **Official**. A template here becomes official only by being
merged into `main` through a reviewed pull request. The signing workflow then signs every template and publishes them as a
release, and the builder checks each signature against the public key built into it.

## How it works

- `templates/<id>/` holds one template: its `template.json` and any files it brings (for example `overlay/`).
- `template.sig` is written only by the signing workflow. It holds the key's id and an ECDSA P-256 signature of the folder: the
  SHA-256 of every file, sorted by path. Changing, adding, removing or renaming any file breaks it, and a `.sig` copied into
  another folder does not fit it.
- The private key is a secret of the GitHub environment `template-signing`. Nobody keeps it on a computer. The workflow writes it
  to a temporary file, signs, and deletes the file.
- The builder holds only the public half. Anyone can read the public half and every `template.sig`; neither lets anyone sign.
- `tools/sign-template.cs` is the only signing code. It never ships with the builder.

## Joining or leaving the team

Joining means being given review rights on this repository and being made a required reviewer of the `template-signing`
environment. Leaving means losing both. No key changes hands.

## Setting up (once, by an owner of the repository)

1. **Branch protection on `main`.** Require a pull request with at least one approving review, require review from code
   owners if you use them, and include administrators. This also protects `.github/workflows`, which could otherwise be changed
   to send the key elsewhere.
2. **Environment `template-signing`** (Settings, Environments):
   - deployment branches: `main` only;
   - required reviewers: the team;
   - variable `TEMPLATE_KEY_ID`, for example `team-2026`;
   - secret `TEMPLATE_SIGNING_KEY`, made in step 3.
3. **The key.** On a trusted computer, with the .NET 10 SDK:

   ```
   dotnet run tools/sign-template.cs -- keygen team-2026.pem
   ```

   It prints the public half. Paste the contents of `team-2026.pem` into the secret `TEMPLATE_SIGNING_KEY`, then delete the file
   for good (empty the recycle bin). Send the printed public half and the key id to whoever builds the Image Builder: it goes
   into `TemplateSignature.Trusted` as an Official key.
4. **Actions permissions.** Allow GitHub Actions, and let workflows create releases (Settings, Actions, General, Workflow
   permissions: read and write).

## Changing a template

1. Open a pull request with the change. The check workflow runs without any secret.
2. A team member reviews and merges it.
3. The signing workflow waits for a required reviewer to approve the `template-signing` environment, signs every template, and
   publishes a release `templates-<date>-<run>` with `templates.zip`.

## Replacing the key

Make a new key with a new id (step 3), and have the builder trust both keys for a release. Then change the secret and the
variable, and run the signing workflow by hand (Actions, Sign templates, Run workflow). Once builders trust the new key, the old
one can be dropped from `TemplateSignature.Trusted`.
