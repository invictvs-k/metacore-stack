import Ajv from "ajv/dist/2020.js";
import addFormats from "ajv-formats";
import { readFile } from "node:fs/promises";
import { globby } from "globby";
import path from "node:path";
import process from "node:process";

const ajv = new Ajv({ strict: true, allowUnionTypes: true, allErrors: false });
addFormats(ajv);
ajv.addKeyword({ keyword: "$metadata" });

const base = new URL(".", import.meta.url).pathname;
const load = async (p) => JSON.parse(await readFile(path.join(base, p), "utf8"));

const schemas = await Promise.all([
  "common.defs.json",
  "room.schema.json",
  "entity.schema.json",
  "message.schema.json",
  "artifact-manifest.schema.json"
].map(load));

// register schemas
schemas.forEach(s => ajv.addSchema(s));

const rootSchemaIds = schemas
  .map(s => s.$id)
  .filter(id => id && id !== "common.defs.json");

// validate examples
const badFiles = await globby(["examples/invalid/*.json"], { cwd: base });

const mustValidate = {
  "examples/room-min.json": "room.schema.json",
  "examples/entity-human.json": "entity.schema.json",
  "examples/message-command.json": "message.schema.json",
  "examples/artifact-sample.json": "artifact-manifest.schema.json"
};

let failures = 0;

for (const [file, schemaId] of Object.entries(mustValidate)) {
  const data = await load(file);
  const validate = ajv.getSchema(schemaId);
  if (!validate) throw new Error(`Schema not found: ${schemaId}`);
  const valid = validate(data);
  if (!valid) {
    failures++;
    console.error(`❌ ${file} failed:`, validate.errors);
  } else {
    console.log(`✅ ${file} ok`);
  }
}

for (const file of badFiles) {
  const data = await load(file);
  // try all schemas; expect at least one failure message to be present
  const anyValid = rootSchemaIds.some(id => {
    const v = ajv.getSchema(id);
    return v && v(data);
  });
  if (anyValid) {
    failures++;
    console.error(`❌ invalid example passed: ${file}`);
  } else {
    console.log(`✅ invalid example rejected: ${file}`);
  }
}

if (failures) {
  console.error(`\n${failures} validation failure(s).`);
  process.exit(1);
} else {
  console.log("\nAll schema validations passed.");
}
