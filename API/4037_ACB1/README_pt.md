# Estratégia ACB1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia ACB1** é a porta StockSharp do consultor especialista MetaTrader distribuída como `MQL/8586/ACB1.MQ4`. O sistema original negocia o par EURUSD e espera por fortes rompimentos diários antes de entrar no mercado. Esta conversão reproduz o mesmo processo de decisão com StockSharp primitivas de alto nível:

- As velas diárias (`SignalCandleType`) definem a direção do rompimento e fornecem as âncoras de stop e take-profit.
- Velas H4 (`TrailCandleType`) determinam a distância final que é multiplicada por `TrailFactor`.
- As ordens são executadas no mercado quando as condições de rompimento são satisfeitas e a estratégia mantém apenas uma posição líquida, espelhando as verificações `OrdersTotal()` no código MQL.
- Stop-loss e take-profit são gerenciados internamente: a estratégia observa os melhores preços de compra/venda e fecha a posição com ordens de mercado quando os níveis de proteção virtuais são violados.

## Regras de negociação

1. **Configuração longa**
   - Use a vela diária finalizada anteriormente.
   - Se `Close > (High + Low) / 2` *e* o preço de venda atual estiver acima da máxima anterior, abra uma posição longa no mercado.
   - O stop loss é colocado no mínimo anterior (arredondado para a etapa do preço do instrumento).
   - O take-profit é igual ao preço de entrada mais `(High − Low) × TakeFactor`.

2. **Configuração curta**
   - Se `Close < (High + Low) / 2` *e* o preço de oferta atual estiver abaixo do mínimo anterior, abra uma posição curta no mercado.
   - Stop-loss é definido para a máxima anterior; o take-profit subtrai `(High − Low) × TakeFactor` do preço de entrada.

3. **Parada final**
   - Os `TrailCandleType` suprimentos de vela finalizados mais recentes `(High − Low) × TrailFactor`.
   - Para posições longas, o stop segue `Bid − TrailDistance` enquanto o preço permanece abaixo do take-profit menos o nível de stop do corretor.
   - Para posições curtas, o stop segue `Ask + TrailDistance` enquanto o preço permanece acima do take-profit mais o nível de stop do corretor.

4. **Guarda de risco**
   - A estratégia acompanha o patrimônio máximo observado do portfólio. A negociação é interrompida sempre que o patrimônio atual cai abaixo de 50% desse pico, exatamente como no consultor original.
   - Um resfriamento de cinco segundos (`CooldownSeconds`) evita novos pedidos ou interrompe atualizações com muita frequência, reproduzindo o acelerador `TimeLocal()` de MQL.

## Dimensionamento de posição e controle de risco

- O volume por negociação é derivado de `Portfolio.CurrentValue × RiskFraction`.
- O risco monetário por contrato é calculado a partir da distância de parada e dos metadados de segurança (`PriceStep` e `StepPrice`).
- O tamanho resultante é alinhado a `Security.VolumeStep` e fixado a `[Security.MinVolume, Security.MaxVolume]`, então limitado pelo parâmetro `MaxVolume` (padrão 5 lotes).
- Os pedidos são ignorados quando o volume normalizado é zero ou quando a distância de parada viola `MinStopDistancePoints`, que emula a verificação MetaTrader `MODE_STOPLEVEL`.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `SignalCandleType` | Diariamente | Tipo de vela usado para detecção de fuga. |
| `TrailCandleType` | 4 horas | Tipo de vela que fornece a distância do trailing stop. |
| `TakeFactor` | 0,8 | Multiplicador aplicado ao intervalo diário para calcular o lucro. |
| `TrailFactor` | 10 | Multiplicador aplicado ao intervalo final ao atualizar o stop. |
| `RiskFraction` | 0,05 | Fração do patrimônio do portfólio arriscado em cada negociação (5%). |
| `MaxVolume` | 5 | Limite rígido para o volume final do pedido. |
| `MinStopDistancePoints` | 0 | Distância mínima de parada/tomada expressa em faixas de preço; configure-o para o corretor `MODE_STOPLEVEL`. |
| `CooldownSeconds` | 5 | Atraso mínimo entre ações comerciais consecutivas. |

## Notas de implementação

- A estratégia requer metadados de instrumento adequados: `Security.PriceStep`, `Security.StepPrice`, `Security.VolumeStep`, `Security.MinVolume` e (se disponível) `Security.MaxVolume`.
- Os níveis de proteção são virtuais. StockSharp fecha posições por meio de ordens de mercado quando a oferta/venda toca o stop-loss ou o take-profit calculado.
- O acompanhamento de patrimônio usa `Portfolio.CurrentValue`. Caso o conector não forneça este campo o Risk Guard manterá a negociação desabilitada até que esteja disponível.
- Apenas uma única posição líquida é mantida. Os sinais opostos enquanto uma negociação está ativa são ignorados até que a posição seja totalmente fechada.
- Nenhuma porta Python está incluída; este diretório contém apenas a implementação e documentação do C#.
