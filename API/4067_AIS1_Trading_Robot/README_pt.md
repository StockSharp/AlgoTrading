# Robô comercial AIS1 (conversão MQL/8700)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **AIS1 Trading Robot** é uma conversão direta em C# do MetaTrader 4 consultor especialista de `MQL/8700/AIS1.MQ4`. O sistema original é adaptado para rompimentos diários do EURUSD e usa intervalos de vários intervalos de tempo para cálculos de stop, target e trailing. Esta implementação StockSharp preserva a estrutura do robô legado enquanto expõe cada elemento configurável como parâmetros de estratégia.

## Lógica de negociação
- **Prazos**
  - Velas primárias: barras de 1 dia para condições de entrada, stop loss e distâncias de take-profit.
  - Velas secundárias: barras de 4 horas para cálculos dinâmicos de trailing stop.
- **Condições de entrada**
  - Longo rompimento: o fechamento diário de ontem está acima do ponto médio da barra e a oferta atual perfura a máxima diária anterior.
  - Breakout curto: o fechamento diário de ontem está abaixo do ponto médio e o lance atual cai abaixo do mínimo diário anterior.
  - Apenas uma posição poderá ser aberta por vez; sinais opostos são ignorados até que a negociação atual seja fechada.
- **Risco e recompensa inicial**
  - Stop loss = máxima/mínima diária anterior ± `StopFactor × daily range`.
  - Take Profit = preço de entrada ± `TakeFactor × daily range`.
  - Ambos os níveis são validados em relação ao `StopBufferTicks` opcional para respeitar as restrições de distância de parada do corretor.
- **Parada final**
  - Usa o intervalo da última vela de 4 horas multiplicado por `TrailFactor`.
  - As atualizações finais exigem que o preço se mova pelo menos `TrailStepMultiplier × spread` além do stop existente e fique longe da meta pelo buffer configurado.
  - A proteção contra saque desativa as atualizações finais quando o patrimônio cai abaixo do limite de reserva.
- **Gerenciamento de Riscos**
  - O tamanho do lote é derivado de `OrderReserve × equity` dividido pelo risco monetário entre entrada e stop.
  - Os volumes são limitados aos limites de troca (`MinVolume`, `MaxVolume`, `VolumeStep`).
  - O monitoramento do patrimônio rastreia o máximo em execução e bloqueia novas entradas quando o patrimônio cai abaixo de `AccountReserve - OrderReserve` desse pico.
- **Proteção de tempo**
  - As ações (entradas ou atualizações finais) são separadas por uma pausa obrigatória de cinco segundos, replicando o acelerador EA original.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `AccountReserve` | 0,20 | Fração do patrimônio que deve permanecer intocada. Usado para calcular o rebaixamento permitido. |
| `OrderReserve` | 0,04 | Fração do patrimônio alocado para cada negociação e base para dimensionamento da posição. |
| `PrimaryCandleType` | Diariamente | Tipo de vela usado para lógica de breakout e alvos estáticos. |
| `SecondaryCandleType` | 4 horas | Tipo de vela usado para derivar distâncias finais. |
| `TakeFactor` | 0,8 | Multiplicador da faixa diária aplicada para obter lucro. |
| `StopFactor` | 1,0 | Multiplicador da faixa diária aplicado ao stop loss. |
| `TrailFactor` | 5,0 | Multiplicador do intervalo de 4 horas aplicado aos trailing stops. |
| `TrailStepMultiplier` | 1,0 | Multiplicador de spread que controla quanto o preço deve avançar antes que um novo trailing stop seja definido. |
| `StopBufferTicks` | 0 | Etapas de preço adicionais adicionadas como margens de segurança em torno de stops e metas. |

## Notas de uso
1. Atribua o **título** desejado (EURUSD por padrão) e o **portfólio** antes de iniciar a estratégia.
2. Certifique-se de que as fontes de velas diárias e de 4 horas estejam disponíveis; caso contrário, os módulos breakout e trailing não poderão ser ativados.
3. A estratégia assina a carteira de pedidos para obter os preços atuais de compra/venda. Em mercados sem feed de profundidade, o último preço negociado é usado como alternativa.
4. As saídas de posição são realizadas por meio de ordens de mercado quando as condições de parada ou meta são atendidas, correspondendo ao comportamento do MetaTrader EA que modificou as ordens de proteção no lado do servidor.
5. O limitador de rebaixamento, o temporizador de pausa e a lógica de dimensionamento de risco podem ser ajustados por meio dos parâmetros expostos para adaptar o robô a diferentes corretores ou especificações de contrato.

## Diferenças vs. Original MQL
- As paradas e metas de proteção são emuladas por fechamentos manuais de posição quando os preços cruzam os níveis armazenados (o MT4 tratou disso por meio da modificação da ordem).
- A conversão de risco depende de `PriceStep` e `StepPrice` do objeto `Security`. Quando esses metadados estão faltando, o código volta para uma conversão monetária 1:1, portanto os usuários devem verificar novamente as especificações do contrato.
- Comentários extensos e descrições de parâmetros foram adicionados para maior clareza e melhor integração com as ferramentas de otimização do StockSharp.

## Requisitos
- StockSharp API de alto nível com acesso a assinaturas de velas e dados do livro de pedidos.
- Conexão comercial configurada corretamente para colocação de pedidos e avaliação de portfólio.
