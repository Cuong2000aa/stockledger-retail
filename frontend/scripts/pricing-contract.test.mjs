import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import assert from "node:assert/strict";

function roundCurrency(value) {
  return Math.round(value * 10000) / 10000;
}

function calcPriceAfterVat(priceBeforeVat, vatRate) {
  return roundCurrency(priceBeforeVat * (1 + vatRate / 100));
}

function calcPriceBeforeVat(priceAfterVat, vatRate) {
  const divisor = 1 + vatRate / 100;
  return divisor === 0 ? priceAfterVat : roundCurrency(priceAfterVat / divisor);
}

const root = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const cases = JSON.parse(
  readFileSync(join(root, "shared", "pricing-contract-cases.json"), "utf8")
);

for (const contractCase of cases) {
  const label = JSON.stringify(contractCase);
  test(`pricing contract ${label}`, () => {
    if (contractCase.priceBeforeVat != null) {
      assert.equal(
        calcPriceAfterVat(contractCase.priceBeforeVat, contractCase.vatRate),
        contractCase.priceAfterVat
      );
    } else if (contractCase.priceAfterVat != null) {
      assert.equal(
        calcPriceBeforeVat(contractCase.priceAfterVat, contractCase.vatRate),
        contractCase.priceBeforeVat
      );
    }
  });
}
