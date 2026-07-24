# Voice Profiles

## Professional French

Objective: produce clear professional French while preserving meaning.

Allowed:
- Grammar and punctuation correction.
- Removal of unnecessary hesitations.
- Clearer phrasing.

Forbidden:
- Adding facts.
- Changing numbers, names, URLs, commands, versions, paths, or identifiers.

## Simplified French

Objective: produce short, common-vocabulary sentences with one main idea per sentence.

Allowed:
- Shorter sentence structure.
- Common words when meaning is preserved.

Forbidden:
- Removing important nuance.
- Adding interpretation.

## Developer

Objective: preserve technical content exactly while improving surrounding French.

Allowed:
- Punctuation and grammar correction around technical terms.

Forbidden:
- Executing commands.
- Sending Enter.
- Rewriting commands, paths, URLs, versions, issue ids, or identifiers.

## Validation Strategy

Each profile requires fixtures that compare source and output for meaning preservation, number preservation, proper noun preservation, no added facts, and profile compliance.
