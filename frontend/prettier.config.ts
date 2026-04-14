import type { Config } from "prettier";

const config: Config = {
  singleQuote: true,
  jsxSingleQuote: true,
  importOrder: ["^react$", "<THIRD_PARTY_MODULES>", "^@bikeapelago(.*)$", "^[~/]", "^[\.?./]"],
  plugins: ["@trivago/prettier-plugin-sort-imports"],
  importOrderSeparation: true,
  importOrderSortSpecifiers: true,
  importOrderParserPlugins: ["typescript", "jsx"],
};

export default config;
