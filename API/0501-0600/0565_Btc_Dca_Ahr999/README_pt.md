# BTC DCA AHR999

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Usa velas diárias UTC concluídas. Os últimos `Length` fechamentos positivos, incluindo o atual, fornecem `G=exp(mean(ln(C)))`. A idade do BTC são dias inteiros desde 2009-01-03: `F=10^(5.84*log10(age)-17.01)` e `AHR999=(C/G)*(C/F)`. Exige a janela completa.

Na segunda-feira permitida, compra o orçamento `UsdInvest2` se o índice for estritamente menor que 0.45; caso contrário, usa `UsdInvest1` se for menor que 1.2. Outros valores, dias e datas não compram. Os limites UTC são inclusivos, com uma decisão por vela diária. Quantidade: `floor(budget/C/VolumeStep)*VolumeStep`; volumes abaixo do mínimo são ignorados. O orçamento usa o fechamento do sinal; o preço executado a mercado pode variar. Ordens pendentes bloqueiam duplicações. Acumula posições sem saída automática.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | `1d` |
| `Length` | `200` |
| `UsdInvest1` | `100` |
| `UsdInvest2` | `1000` |
| `StartDate` | `2024-02-01T00:00:00Z` |
| `EndDate` | `2025-12-31T00:00:00Z` |

## Referências

- [AHR999 geometric-cost implementation](https://github.com/alibaba-web3/quant-master/blob/main/ahr999_index.py)
