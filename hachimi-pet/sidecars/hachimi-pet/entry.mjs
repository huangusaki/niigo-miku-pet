import { launchPet } from '../../scripts/launch-pet.mjs';

process.argv.push('--foreground');
const code = await launchPet();
process.exit(code);
