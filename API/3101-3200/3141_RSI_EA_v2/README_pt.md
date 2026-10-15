# Estratégia de RSI EA v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do assessor especialista MetaTrader 5 **"RSI EA v2"**. Automatiza o trading em torno dos cruzamentos de limiar do Relative Strength Index (RSI) enquanto replica os controles de gerenciamento de dinheiro, trailing stop e janela de trading do assessor original. Por padrão, a estratégia processa velas de um minuto, mas qualquer tipo de vela pode ser fornecido através de parâmetros.

## Lógica de negociação

- **Condições de entrada**
  - Posições longas abrem quando o RSI sobe acima do *nível de Compra* configurado após estar abaixo na vela finalizada anterior, e as horas de trading permitem novas ordens.
  - Posições curtas abrem quando o RSI cai abaixo do *nível de Venda* configurado após estar acima anteriormente, e a janela de trading está aberta.
  - Quando já existe uma posição oposta, a estratégia dimensiona a nova ordem de mercado para aplanar a exposição atual e estabelecer a direção solicitada (somente posições líquidas).
- **Condições de saída**
  - Os níveis de stop-loss e take-profit expressos em pips são anexados assim que uma nova posição é detectada.
  - Um trailing stop imita o EA original: ativa-se após o preço avançar *Trailing stop + Trailing step* e então se move pelo menos o passo de trailing.
  - A lógica opcional "fechar por sinal" sai de posições longas quando o RSI cruza para baixo através do nível de venda, e sai de posições curtas quando o RSI cruza para cima através do nível de compra.
  - Stops e sinais são avaliados apenas em velas finalizadas, mantendo o comportamento determinístico em backtests.

## Gestão de risco e negociação

- **Stop-loss / Take-profit** – definidos em pips, convertidos em incrementos de preço que respeitam a precisão do instrumento (incluindo símbolos forex de 3/5 decimais).
- **Trailing stop** – desabilitado quando a distância é zero. Um passo de trailing positivo é necessário sempre que a distância de trailing for diferente de zero.
- **Dimensionamento de posição** – um volume fixo ou um volume automático calculado a partir do percentual de risco e distância do stop. O dimensionamento de risco requer acesso ao patrimônio do portfólio e metadados de passo de preço válidos.
- **Janela de trading** – filtro diário opcional definido por horas de início inclusivas e fim exclusivas (0–23). Quando início é igual a fim, o mercado é considerado fechado.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `OpenBuy` / `OpenSell` | Ativa/desativa entradas longas ou curtas independentemente. |
| `CloseBySignal` | Habilita saídas em cruzamentos de RSI opostos. |
| `StopLossPips` | Distância do stop-loss em pips (0 desabilita o stop). |
| `TakeProfitPips` | Distância do take-profit em pips (0 desabilita a meta). |
| `TrailingStopPips` | Distância do trailing stop em pips. Deve ser zero se nenhum trailing for desejado. |
| `TrailingStepPips` | Progresso adicional (em pips) necessário antes de mover o trailing stop. Deve ser positivo quando o trailing estiver ativo. |
| `RsiPeriod` | Comprimento do indicador RSI. |
| `RsiBuyLevel` / `RsiSellLevel` | Limiares para entradas/saídas longas e curtas. |
| `UseRiskSizing` | Alterna entre volume fixo e dimensionamento por percentual de risco. |
| `FixedVolume` | Tamanho base da ordem para modo de volume fixo ou fallback quando o dimensionamento de risco não pode ser calculado. |
| `RiskPercent` | Percentual do patrimônio do portfólio arriscado por negociação. Usado somente quando `UseRiskSizing` é verdadeiro e existe distância de stop positiva. |
| `UseTimeControl` | Habilita o filtro de janela de trading diária. |
| `StartHour` / `EndHour` | Hora de início inclusiva e fim exclusiva (0–23) da janela de trading. |
| `CandleType` | Tipo de dados de vela que impulsiona os cálculos do indicador. |

## Notas de implementação

- Usa a API de assinatura de velas de alto nível com binding do indicador `RSI`.
- Converte distâncias em pips usando a precisão do instrumento (`PriceStep` e `Decimals`) para corresponder à lógica de 3/5 dígitos do MetaTrader.
- Normaliza volumes de ordens para o step de volume e limites do instrumento (volume mín/máx).
- A lógica de trailing apenas atualiza as referências de stop internas; as saídas são realizadas com ordens de mercado quando os níveis são violados.
- Mantém estado separado para posições longas e curtas para preservar os níveis de trailing e protetores entre velas.

## Uso

1. Anexe a estratégia a um conector StockSharp com metadados de instrumento e portfólio apropriados.
2. Configure os limiares, distâncias em pips e janela de tempo opcional para corresponder ao mercado desejado.
3. Habilite o dimensionamento baseado em risco se as informações do portfólio estiverem disponíveis; caso contrário, deixe desabilitado para usar um lote fixo.
4. Inicie a estratégia – ela aguardará velas finalizadas, aplicará a lógica RSI e gerenciará posições ativas de acordo com as proteções configuradas.
