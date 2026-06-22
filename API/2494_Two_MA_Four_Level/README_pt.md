# Estratégia de Dois MA Quatro Níveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista MetaTrader "2MA_4Level" usando a API de alto nível do StockSharp. Ela opera um único instrumento com duas médias móveis suavizadas (SMMA) calculadas sobre o preço mediano e observa cinco zonas relativas de cruzamento entre as curvas rápida e lenta. As entradas só são permitidas quando não há posição aberta, e cada operação é protegida por offsets de stop-loss e take-profit em pips.

## Lógica

- Uma SMMA rápida e uma lenta são calculadas na série de velas selecionada (padrão 50 e 130 períodos).
- Os valores anteriores e atuais da SMMA na vela completada são avaliados para detectar um cruzamento.
- O cruzamento é verificado contra cinco limiares construídos a partir da MA lenta:
  - a MA lenta pura (sem offset),
  - MA lenta + pips de `MostTopLevel`,
  - MA lenta + pips de `TopLevel`,
  - MA lenta - pips de `LowermostLevel`,
  - MA lenta - pips de `LowerLevel`.
- Quando a MA rápida cruza acima de qualquer limiar, uma posição comprada é aberta (se flat). Um cruzamento abaixo de qualquer limiar abre uma posição vendida.
- Os níveis de stop-loss e take-profit são anexados através de `StartProtection` usando o valor de pip do instrumento (`Security.PriceStep`).

A estratégia nunca piramida posições: uma nova operação só pode ser aberta após o fechamento da anterior por stop ou objetivo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `FastPeriod` | 50 | Comprimento da média móvel suavizada rápida. Deve ser menor que `SlowPeriod`. |
| `SlowPeriod` | 130 | Comprimento da média móvel suavizada lenta. |
| `MostTopLevel` | 500 | Offset superior (em pips) para a confirmação altista/baixista mais ampla. Deve ser maior que `TopLevel`. |
| `TopLevel` | 250 | Offset superior (em pips) para a confirmação altista/baixista secundária. |
| `LowerLevel` | 250 | Offset inferior (em pips) para a confirmação baixista/altista secundária. Deve ser menor que `LowermostLevel`. |
| `LowermostLevel` | 500 | Offset inferior (em pips) para a confirmação baixista/altista mais ampla. |
| `TakeProfitPips` | 55 | Distância da entrada ao take-profit, expressa em pips. |
| `StopLossPips` | 260 | Distância da entrada ao stop-loss, expressa em pips. |
| `CandleType` | Período de 15 minutos | Série de velas usada para os cálculos de SMMA e processamento de sinais. |

## Detalhes de implementação

- O preço mediano (`(High + Low) / 2`) alimenta ambas as SMMAs, correspondendo à configuração do MT5 que usa `PRICE_MEDIAN`.
- O teste de cruzamento compara a última vela completada com a anterior, eliminando qualquer dependência de barras parcialmente formadas.
- `StartProtection` conecta o stop-loss e take-profit uma única vez na inicialização, de modo que cada ordem herda automaticamente os limites de risco configurados.
- A estratégia para durante `OnStarted` se combinações de parâmetros inválidas forem fornecidas (ex.: `FastPeriod >= SlowPeriod`).

## Notas de uso

1. Vincule a estratégia a um instrumento com `PriceStep` definido; caso contrário, a conversão de pip recai para o valor `1`.
2. Adequada para contas de hedge no MT5; no StockSharp comporta-se da mesma forma ao garantir apenas uma posição aberta por vez.
3. Os hooks de otimização (`SetCanOptimize`) estão habilitados para ambos os períodos de MA, permitindo executar varreduras de parâmetros diretamente do otimizador StockSharp.
4. Como a estratégia depende exclusivamente de saídas por stop-loss e take-profit, garanta que as distâncias configuradas estejam alinhadas com a volatilidade do instrumento para evitar exposição prolongada.

## Arquivos

- `CS/TwoMaFourLevelStrategy.cs` – Implementação em C# da lógica de trading.
- `README_ru.md` – Documentação em russo.
- `README_zh.md` – Documentação em chinês.
