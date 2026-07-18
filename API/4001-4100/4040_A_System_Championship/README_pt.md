# Uma estratégia de campeonato de sistema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porta do consultor especialista MetaTrader 4 "A System: Championship Strategy Final Edit" (arquivo `ACB6.MQ4`).
- Detecta rompimentos de alta ou baixa em um período primário configurável e confirma a dinâmica com preços de compra/venda em tempo real.
- Usa um período de tempo secundário para dimensionar a distância do trailing stop, reproduzindo a lógica multithread do EA original por meio de dois fluxos de velas.
- Implementa os blocos globais de parada de ações, pausa comercial e dimensionamento de risco adaptativo que foram codificados no robô de origem.

## Assinaturas de dados
- Assina duas séries de velas (`PrimaryTimeFrame`, `SecondaryTimeFrame`) para reconstruir as faixas de preço usadas para alvos e trailing stops.
- Assina as cotações de nível 1 para ler o melhor bid/ask que aciona entradas, verificações de stop-loss, take-profit e saída de retração.

## Condições de entrada
1. Aguarde o término da vela primária e calcule seu intervalo multiplicado por `TakeFactor`.
2. Vá comprado quando:
   - A vela fecha acima do seu ponto médio.
   - O preço de venda atual rompe a máxima da vela.
   - A distância entre o lance e o mínimo da vela excede `MinStopDistance`.
3. Opere vendido quando as condições espelhadas forem verdadeiras para o rompimento negativo.
4. Ignore as entradas se a distância de lucro calculada for menor que o espaçamento mínimo de stop.

## Gerenciamento de saída
- **Níveis de proteção iniciais**: o stop está ancorado na mínima/máxima da vela anterior, enquanto o take-profit é igual ao preço de entrada mais/menos o intervalo multiplicado por `TakeFactor`.
- **Saída de retração** (`FallLimit`/`FallFactor`):
  - Acompanhe a excursão mais favorável para a posição ativa.
  - Se o movimento atual cair abaixo de `FallLimit * maxMove` *e* o movimento já avançou além de `FallFactor * target`, feche a negociação no mercado.
- **Parada móvel** (`TrailFactor`):
  - A distância final é igual ao intervalo do período secundário multiplicado por `TrailFactor`.
  - O stop se move apenas na direção comercial e nunca cruza o take-profit ou o espaçamento mínimo do stop.
- **Paradas bruscas**: o preço tocando os níveis de stop ou take mantidos resulta em liquidação imediata usando ordens de mercado.

## Gestão de risco
- **Dimensionamento de posição dinâmico**: combina `RiskPerTrade` com o valor pip derivado de `Security.StepSize` e `Security.StepPrice`. O volume resultante é arredondado para restrições cambiais e nunca fica abaixo de `BaseVolume`.
- **Ajuste estatístico**: o índice `LossesExpected/TradesExpected` do EA original modula o risco por negociação comparando-o com o índice de perdas realizadas.
- **Equity stop** (`SystemStop`): rastreia o pico do patrimônio e desativa novas negociações se o valor atual cair abaixo de `SystemStop * peak`. Os registros informativos marcam a parada de ativação e recuperação.
- **Pausa comercial** (`TradePause`): impõe uma janela de resfriamento após cada ordem de mercado, assim como a implementação MT4.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `PrimaryTimeFrame` | 1 dia | Prazo mais alto usado para detecção de fuga. |
| `SecondaryTimeFrame` | 4 horas | Período que fornece o intervalo do trailing stop. |
| `TakeFactor` | 0,8 | Multiplicador aplicado ao intervalo da vela primária ao criar o take-profit. |
| `TrailFactor` | 10 | Multiplicador aplicado ao intervalo secundário da vela ao atualizar o trailing stop. |
| `FallLimit` | 0,5 | Proporção do lucro máximo que permite a saída da retração. |
| `FallFactor` | 0,4 | Parcela mínima do alvo completo que deve ser alcançada antes que uma saída de retração seja permitida. |
| `RiskPerTrade` | 0,02 | Fração do patrimônio alocado em cada negociação antes dos ajustes. |
| `BaseVolume` | 1 | Tamanho do pedido substituto usado quando o dimensionamento do risco produz um volume menor. |
| `MinStopDistance` | 0 | Restrição de distância de parada cambial expressa em unidades de preço. |
| `TradePause` | 5 minutos | Período de espera após qualquer ordem executada. |
| `SystemStop` | 0,8 | Fator de rebaixamento para o stop de patrimônio do portfólio (por exemplo, 0,8 = 20% de rebaixamento permitido). |
| `LossesExpected` | 20 | Número esperado de negociações perdidas para ajuste de risco. |
| `TradesExpected` | 85 | Número esperado de negociações totais para ajuste de risco. |

## Notas de implementação
- Os threads de bloqueio/cobertura da versão MQL são omitidos porque as estratégias StockSharp operam em uma posição líquida. O controle de risco e a lógica de rastreamento fornecem um mecanismo equivalente de proteção de capital.
- Os níveis de stop e take são rastreados dentro da estratégia, em vez de usar ordens nativas separadas para permanecerem alinhados com o mecanismo de backtesting.
- Certifique-se de que a segurança conectada exponha `StepSize`, `StepPrice`, `MinVolume` e `VolumeStep`; caso contrário, o dimensionamento voltará para `BaseVolume`.
- A estratégia deve funcionar com cotações em tempo real habilitadas; caso contrário, apenas as atualizações acionadas por velas serão executadas e a lógica de parada reagirá com a latência das velas.
