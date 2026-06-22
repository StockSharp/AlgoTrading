# Estratégia Stoch Komposter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do expert MQL5 **Exp_iStochKomposter**. Utiliza o Oscilador Estocástico para detectar reversões de momentum e negocia quando a linha %K cruza limites predefinidos.

## Como Funciona

- Calcula o Oscilador Estocástico no período selecionado.
- Gera um sinal de **compra** quando %K cruza acima do nível inferior (padrão 30).
- Gera um sinal de **venda** quando %K cruza abaixo do nível superior (padrão 70).
- Em cada sinal, a estratégia fecha qualquer posição oposta e abre uma nova posição na direção do sinal usando ordens a mercado.
- Níveis opcionais de stop loss e take profit são aplicados via `StartProtection`.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `KPeriod` | Período de cálculo da linha %K | 5 |
| `DPeriod` | Período de suavização da linha %D | 3 |
| `UpLevel` | Limiar de sobrecompra para acionar vendas | 70 |
| `DownLevel` | Limiar de sobrevenda para acionar compras | 30 |
| `StopLoss` | Stop loss absoluto em unidades de preço | 1000 |
| `TakeProfit` | Take profit absoluto em unidades de preço | 2000 |
| `CandleType` | Período para cálculos | 1 hora |

## Notas

- A estratégia opera apenas em velas finalizadas.
- Não calcula os níveis ATR do indicador original; eram usados apenas para posicionamento de setas na versão MQL.
- O tamanho da posição é definido pela propriedade `Volume` da estratégia.
