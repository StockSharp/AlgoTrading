# Estratégia Constituents EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o **Constituents EA** de `MQL/22595` para a API de alto nível do StockSharp. Ela recria a lógica original
de colocar duas ordens pendentes em torno do intervalo mais recente em uma hora específica, mantendo o fluxo de trabalho
compatível com o manuseio de ordens e os helpers de proteção de risco do StockSharp.

## Como a estratégia funciona

1. **Ativação programada** – ao final de cada candle a estratégia verifica se a próxima barra começará em `StartHour`. Apenas
   nesse momento são consideradas novas ordens pendentes, o que replica o código do MetaTrader que reagia ao nascimento da barra
   cujo tempo de abertura coincide com a hora configurada.
2. **Detecção de intervalo** – a maior máxima e a menor mínima entre os `SearchDepth` candles concluídos anteriores são
   rastreadas com indicadores `Highest`/`Lowest`. Esses dois preços definem os níveis de ruptura/reversão à média usados para
   colocação de ordens.
3. **Filtros de distância de preço** – as melhores cotações de bid/ask atuais são transmitidas do feed do livro de ordens. As
   ordens são colocadas apenas se a distância entre a cotação e o preço candidato for maior ou igual a `MinOrderDistancePips`
   (convertido para preço absoluto usando `PointValue`). Isso reimplementa a validação do nível de congelamento original e
   evita ordens pendentes inválidas.
4. **Seleção de estilo de ordem** – `PendingOrderMode` escolhe entre ordens limitadas (buy limit na mínima, sell limit na
   máxima) ou ordens stop (buy stop acima da máxima, sell stop abaixo da mínima). Ambas as ordens são enviadas
   simultaneamente, assim como no script do MetaTrader.
5. **Proteção de risco** – o helper integrado `StartProtection` anexa níveis de stop-loss e take-profit expressos em passos de
   preço absolutos (`StopLossPips`/`TakeProfitPips`). As verificações de distância mínima contra `MinStopDistancePips`
   replicam o requisito do MT5 de que as ordens protetoras devem respeitar o nível de stop do símbolo.
6. **Gerenciamento de ordens** – se uma ordem pendente for executada, a ordem oposta é cancelada imediatamente. Durante o
   intervalo da barra a estratégia nunca coloca ordens adicionais enquanto existem ordens ativas, correspondendo ao
   comportamento do EA de origem.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `StartHour` | Hora (0-23) quando o novo par de ordens pendentes é criado. |
| `SearchDepth` | Número de candles concluídos anteriores usados para calcular o intervalo máximo/mínimo. |
| `PendingOrderMode` | `Limit` replica a variante de reversão à média, `Stop` coloca ordens de ruptura. |
| `StopLossPips` | Distância de stop-loss medida em pips (convertida com `PointValue`). Definir como 0 para desabilitar. |
| `TakeProfitPips` | Distância de take-profit em pips. Definir como 0 para desabilitar. |
| `PointValue` | Valor do pip em unidades de preço. Definir como 0 para auto-detectar de `Security.PriceStep`/`MinStep`. |
| `MinOrderDistancePips` | Distância mínima permitida entre bid/ask atual e o preço pendente, modelando verificações de freeze-level. |
| `MinStopDistancePips` | Distância mínima permitida para stop/take, espelhando verificações de `StopsLevel`. |
| `CandleType` | Período usado para cálculo de intervalo e lógica de agendamento. |

`Strategy.Volume` controla o tamanho da ordem; mantê-lo positivo para que `BuyLimit`, `SellLimit`, `BuyStop` e `SellStop`
possam enviar ordens.

## Uso

1. Anexar a estratégia a um instrumento e definir `CandleType` para o período que se deseja operar.
2. Configurar `StartHour` e `SearchDepth` exatamente como nas entradas do MT5. Ajustar os limites `Min*Pips` se o broker
   aplicar distâncias mínimas entre ordens e o preço de mercado.
3. Calibrar `PointValue` quando a auto-detecção dos metadados do instrumento não for possível (por exemplo, em instrumentos
   sintéticos).
4. Definir `StopLossPips` e `TakeProfitPips` para corresponder ao EA original. O módulo de proteção anexará automaticamente
   stops e alvos assim que uma ordem for executada.
5. Fornecer um `Volume` positivo e iniciar a estratégia. Ela se inscreverá em dados de candles e livro de ordens, colocará
   ambas as ordens pendentes na barra agendada e cancelará a ordem oposta quando uma operação for executada.

## Diferenças em relação ao EA original

- O modo de risco `MoneyFixedMargin` do MetaTrader (dimensionamento baseado em porcentagem) não está portado. Os usuários do
  StockSharp devem configurar `Strategy.Volume` diretamente ou envolver a estratégia com um módulo externo de dimensionamento
  de posição.
- As verificações de freeze-level e stop-level são expressas através dos parâmetros configuráveis `MinOrderDistancePips` e
  `MinStopDistancePips` porque os metadados equivalentes da exchange nem sempre estão disponíveis através do StockSharp.
- A colocação de ordens ocorre quando o candle anterior fecha e a próxima barra começa em `StartHour`. Isso é funcionalmente
  idêntico à implementação do MT5 que era acionada no nascimento da nova barra.
- Todos os comentários dentro do código fonte foram traduzidos para o inglês, enquanto a documentação externa está disponível
  em vários idiomas por conveniência.

Ajustar as distâncias e a hora de trading para corresponder ao instrumento que se planeja operar. Em mercados com spreads
amplos pode ser necessário aumentar `MinOrderDistancePips` ou valores de pip para evitar rejeição imediata pelo broker.
