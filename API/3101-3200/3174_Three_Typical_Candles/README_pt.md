# Estratégia de Three Typical Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Three Typical Candles** recria o Expert Advisor do MetaTrader "Three Typical Candles" dentro da API de alto nível do StockSharp. O sistema observa o preço típico das três últimas velas concluídas e opera quando detecta uma sequência estritamente monótona. O preço típico é definido como a média aritmética da máxima, mínima e fechamento de uma vela. Quando as três velas finalizadas mais recentes formam uma sequência crescente de preços típicos, a estratégia entra comprada. Uma sequência decrescente dispara uma entrada vendida.

O port segue de perto a lógica MQL5 original:
- Os sinais são avaliados apenas uma vez por vela finalizada para evitar ruído intrabarra.
- Uma janela de trading configurável pode desabilitar o trading fora dos horários selecionados e força a estratégia para posição zerada quando o filtro está ativo.
- Posições opostas são fechadas antes de abrir uma nova, então a estratégia nunca mantém ambas as direções ao mesmo tempo.
- O volume das ordens espelha o EA fonte usando um tamanho de lote fixo, respeitando o passo de volume da bolsa e as restrições de volume mínimo e máximo informadas pelo instrumento.

## Regras de trading
1. **Detecção de sinal**
   - Calcular o preço típico `Tp = (High + Low + Close) / 3` para cada vela finalizada.
   - Rastrear os dois valores típicos anteriores. Com três valores disponíveis, verificar uma sequência estritamente crescente ou decrescente.
2. **Entrada comprada**
   - Se `Tp[-2] < Tp[-1] < Tp[0]` (três preços típicos crescentes) e a posição atual não é comprada, a estratégia fecha qualquer exposição vendida e envia uma ordem de compra a mercado.
3. **Entrada vendida**
   - Se `Tp[-2] > Tp[-1] > Tp[0]` (três preços típicos decrescentes) e a posição atual não é vendida, a estratégia fecha qualquer exposição comprada e envia uma ordem de venda a mercado.
4. **Controle de tempo**
   - Quando o filtro de tempo opcional está habilitado, a estratégia avalia o sinal apenas quando o horário de abertura da vela cai dentro da sessão de trading configurada. Fora dessa janela, qualquer posição aberta é liquidada imediatamente e nenhum novo trade é realizado.
5. **Gestão de posições**
   - A estratégia não tem níveis explícitos de stop-loss ou take-profit. O gerenciamento de risco deve ser tratado externamente (p. ex., via estratégias protetoras ou supervisão manual).

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|--------|-----------|
| `Volume` | decimal | `1` | Volume de ordem fixo (lotes ou contratos). A estratégia arredonda automaticamente o valor para o passo de volume válido mais próximo e aplica os limites mínimo/máximo do instrumento. |
| `UseTimeControl` | bool | `true` | Habilita o filtro de janela de trading intradiário. Quando desabilitado, os sinais são avaliados 24 horas por dia. |
| `StartHour` | int | `11` | Hora de início inclusiva (0-23) da janela de trading quando `UseTimeControl` é verdadeiro. |
| `EndHour` | int | `17` | Hora de fim exclusiva (0-23) da janela de trading quando `UseTimeControl` é verdadeiro. Se a hora de fim for menor que a de início, a janela abrange meia-noite. |
| `CandleType` | `DataType` | `TimeFrame(1h)` | Tipo de vela usado para análise. Selecione um período compatível com seu feed de dados. |

## Notas de implementação
- A classe base `Strategy` do StockSharp gerencia assinaturas e roteamento de ordens. Os sinais são avaliados em `ProcessCandle`, que recebe velas concluídas através da API de binding de alto nível.
- As ordens a mercado são emitidas através de `BuyMarket` e `SellMarket`. Quando ocorre uma reversão, a estratégia primeiro fecha a exposição existente usando uma ordem de mercado oposta antes de enviar a nova entrada.
- `StartProtection()` é chamado durante a inicialização para permitir o anexo de mecanismos de proteção opcionais se desejado.
- O helper `GetTradeVolume` replica a normalização de lotes do MetaTrader ajustando o volume configurado às restrições da bolsa (passo de volume, mínimo e máximo).
- A estratégia armazena apenas dois preços típicos históricos, suficientes para avaliar o padrão de três velas sem manter grandes coleções.

## Dicas de uso
- Anexe a estratégia a um instrumento com liquidez suficiente. O EA original usava dados Forex intradiários, mas qualquer mercado que forneça velas OHLC pode ser usado.
- Escolha um período de velas que se adapte ao seu horizonte de trading. As velas de uma hora padrão replicam o comportamento do EA fonte; intervalos mais curtos ou mais longos podem ser explorados através da otimização de parâmetros.
- Considere combinar a estratégia com controles de risco como limites de drawdown máximo ou stop loss no nível do portfólio via o framework de estratégias protetoras do StockSharp.
- Faça backtests em múltiplos instrumentos e sessões de trading para confirmar que o padrão estritamente monótono produz sinais acionáveis sob suas condições de mercado.
