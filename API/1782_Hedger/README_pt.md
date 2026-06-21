# Estratégia de Cobertura (Hedger)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca uma ordem limitada e uma ordem stop oposta para cobrir a posição inicial. Funciona tanto no modo comprado quanto vendido e incorpora vários controles de risco.

A ordem de cobertura protege a operação principal caso o preço se mova na direção errada. Uma regra de trailing 75-50 pode mover o stop para metade do alvo assim que 75% da meta de lucro for atingida. A cobertura de risco opcional pode abrir uma ordem de mercado contra a posição após um forte movimento adverso, e o stop pode ser ajustado após um número configurável de ticks.

## Detalhes

- **Critérios de entrada**: Colocar ordem limitada em `EntryPrice` e stop de cobertura em `EntryPrice ± Spread`.
- **Comprado/Vendido**: Configurado via `IsLong`.
- **Critérios de saída**: Stop loss, take profit, regra 75-50 ou cobertura de risco.
- **Stops**: Sim, com ajuste opcional.
- **Filtros**: Nenhum.

## Parâmetros

- `EntryPrice` – preço para a ordem pendente.
- `StopLoss` – nível de stop protetor.
- `TakeProfit` – alvo de lucro.
- `Volume` – volume da ordem.
- `Spread` – distância para a ordem de cobertura.
- `IsLong` – operação comprada se verdadeiro, vendida se falso.
- `UseRiskHedge` – abrir ordem de mercado oposta em forte movimento adverso.
- `UseRiskSl` – ajustar stop após movimento adverso de `RiskSlTicks`.
- `RiskSlTicks` – número de ticks para o ajuste do stop de risco.
- `UseRule7550` – ativar regra de trailing 75-50.
