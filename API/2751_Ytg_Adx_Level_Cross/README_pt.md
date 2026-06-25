# Estratégia Ytg ADX Cruzamento de Nível
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o assessor especialista `_ADX.mq5` de Yuriy Tokman para a API de alto nível do StockSharp. Ela monitora o Average Directional Index e reage quando os componentes +DI ou -DI ultrapassam limites configuráveis. As ordens são abertas apenas uma de cada vez, espelhando a lógica MQL original, e os níveis de stop-loss e take-profit de proteção expressos em pontos de preço são aplicados automaticamente.

## Visão geral

- **Regime de mercado**: Funciona em movimentos tendenciais ou fortemente direcionais onde picos de DI acompanham rompimentos.
- **Direção**: Abre posições compradas ou vendidas, mas nunca ambas simultaneamente.
- **Período**: Controlado pelo parâmetro `CandleType` (padrão velas de 1 hora).
- **Dados**: Usa velas finalizadas para calcular valores ADX/DI do indicador `AverageDirectionalIndex`.

## Lógica de trading

1. Assinar a série de velas selecionada e construir o indicador ADX com o `AdxPeriod` configurado.
2. Para cada vela finalizada, coletar os valores +DI e -DI e manter apenas a quantidade de histórico exigida pelo parâmetro `Shift`. Um `Shift` de 1, idêntico ao padrão MQL, avalia a vela fechada anterior.
3. **Entrada comprada**: Ativada quando o valor +DI deslocado sobe acima de `LevelPlus` enquanto seu valor anterior estava abaixo do mesmo limiar. A estratégia verifica que não há posição atualmente aberta antes de comprar a mercado.
4. **Entrada vendida**: Ativada quando o valor -DI deslocado sobe acima de `LevelMinus` enquanto seu valor anterior estava abaixo desse nível. Uma venda a mercado é enviada apenas se não houver posição ativa.
5. As saídas são tratadas exclusivamente por ordens de proteção iniciadas através de `StartProtection`: um take-profit e stop-loss fixos medidos em pontos de preço, equivalentes a `TP` e `SL` do código original.

Esta implementação evita intencionalmente o aumento médio em posições, reentradas enquanto as operações estão ativas, ou filtros adicionais, correspondendo ao comportamento leve do EA fonte.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 1 hora | Período da assinatura de velas usada para o cálculo do ADX. |
| `AdxPeriod` | 28 | Comprimento do Average Directional Index e seus cálculos de DI. |
| `LevelPlus` | 5 | Limiar que a série +DI deve exceder para abrir uma posição comprada. |
| `LevelMinus` | 5 | Limiar que a série -DI deve exceder para abrir uma posição vendida. |
| `Shift` | 1 | Número de velas fechadas a analisar retrospectivamente ao avaliar o cruzamento de DI (1 = vela anterior). |
| `TakeProfitPoints` | 500 | Distância em pontos de preço para a ordem de take-profit. Multiplicada internamente pelo tamanho de tick do instrumento. |
| `StopLossPoints` | 500 | Distância em pontos de preço para a ordem de stop-loss de proteção. |
| `TradeVolume` | 0.1 | Volume base para novas ordens a mercado, correspondendo à configuração `Lots` no especialista MQL. |

## Gestão de risco

- `StartProtection` converte os valores de take-profit e stop-loss baseados em pontos em distâncias de preço absolutas usando o `PriceStep` do instrumento.
- Nenhum trailing stop ou lógica de ponto de equilíbrio é aplicado; as saídas ocorrem exclusivamente através das ordens de proteção configuradas.

## Notas e dicas

- Limites de DI extremamente baixos podem levar a operações de vai-e-vem frequentes, enquanto níveis mais altos esperam por impulsos direcionais mais fortes.
- O parâmetro `Shift` pode ser aumentado quando é necessária confirmação de velas anteriores, por exemplo em períodos maiores para filtrar o ruído intrábarra.
- Como a estratégia opera apenas uma posição por vez, interferências manuais ou operações externas na mesma conta devem ser evitadas para prevenir conflitos com o rastreamento interno de posição.
