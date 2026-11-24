# Community templates

A template for a distribution the official set does not cover, shared by a user and reviewed by the team.

1. Make the template with `mbos template new` and check it with `mbos template check <file>`. Your file is `community/<Name>.json`,
   with one `"versions"` list, and no `// signature:` line.
2. Open a pull request that adds only that file. Its name must differ from every file in `templates/`, and its ids must not be
   an official id or an official script name.
3. A team member reads the file: what it downloads, what it copies, the ROM it names. Nothing is merged that the reviewer has
   not understood.
4. After the merge, the signing workflow signs it with the Community key and publishes it in a release `community-<date>-<run>`
   (`community.zip`). The builder then shows it as **Community**.

A community template is built like a user's own: the builder writes its slot script, and it cannot install from disks or take an
official script name. The badge says the team reviewed the file, not that it is safe to run on anything you care about.
