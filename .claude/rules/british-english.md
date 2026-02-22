# British English Spelling

All English text MUST use **British English** spelling. This applies to:
- Code: variable names, function names, class names, enum values
- Comments and documentation
- UI text, labels, error messages
- Commit messages and PR descriptions
- File and folder names

## Common Differences

| American (WRONG) | British (CORRECT) |
|---|---|
| color | colour |
| favorite | favourite |
| initialize | initialise |
| optimize | optimise |
| customize | customise |
| organize | organise |
| recognize | recognise |
| authorize | authorise |
| analyze | analyse |
| catalog | catalogue |
| center | centre |
| meter | metre |
| fiber | fibre |
| license (noun) | licence (noun) |
| defense | defence |
| offense | offence |
| practice (verb) | practise (verb) |
| dialog | dialogue |
| fulfill | fulfil |
| enrollment | enrolment |
| canceled | cancelled |
| traveled | travelled |
| modeled | modelled |
| labeled | labelled |
| gray | grey |
| aging | ageing |
| judgment | judgement |

## Exceptions

- **Third-party API names**: keep original spelling (e.g., `Color` in CSS, `colorScheme` in React Native, `Optimize` in a library name)
- **Language keywords**: `color` in CSS is a keyword, not a spelling choice — leave as-is
- **Established codebases**: when modifying existing code, match the file's current convention to avoid inconsistency within a single file; flag for future refactor if needed
