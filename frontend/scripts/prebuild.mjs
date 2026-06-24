import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const NEXT_DIR = path.join(ROOT, ".next");
const PORT = Number(process.env.PORT || 3000);

function portInUse(port) {
  if (process.platform === "win32") {
    try {
      const output = execSync("netstat -ano -p tcp", { encoding: "utf8" });
      return output
        .split(/\r?\n/)
        .some((line) => line.includes("LISTENING") && line.includes(`:${port}`));
    } catch {
      return false;
    }
  }

  try {
    execSync(`lsof -ti tcp:${port}`, { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
}

if (portInUse(PORT)) {
  console.error(
    `[prebuild] Port ${PORT} is in use. Stop "npm run dev" before running "npm run build".`
  );
  process.exit(1);
}

fs.rmSync(NEXT_DIR, { recursive: true, force: true });
console.log("[prebuild] Cleared .next before production build");
