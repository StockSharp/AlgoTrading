# Estratégia de ajudante de empresa de apoio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Prop Firm Helper Strategy é um sistema de breakout de canal Donchian convertido do consultor especialista MetaTrader "Prop Firm Helper". A estratégia envia ordens de stop acima do intervalo recente para entradas longas e abaixo do intervalo para entradas curtas. Ele aplica automaticamente as regras de desafio da prop firme, interrompendo a negociação depois que o patrimônio alvo é atingido ou quando o limite de perda diária é violado.

## Lógica de negociação
- Assine velas definidas pelo parâmetro `Candle Type`.
- Calcule dois canais Donchian:
  - `Entry Period`/`Entry Shift` para detectar interrupções.
  - `Exit Period`/`Exit Shift` para rastrear negociações abertas.
- Coloque ordens de compra stop um tick acima da máxima superior deslocada Donchian quando estiver estável ou vendido.
- Coloque ordens de stop de venda um tick abaixo da mínima inferior deslocada Donchian quando estiver plana ou longa.
- Use a suavização Average True Range (`ATR Period`) para decidir quando avançar as ordens de stop.
- Feche as posições longas se a vela ficar abaixo da mínima final Donchian. Feche as posições vendidas quando a vela fechar acima da máxima final de Donchian.

## Gestão de risco
- `Risk Per Trade %` calcula o volume do pedido a partir do patrimônio atual do portfólio, tamanho do passo do instrumento e preço do passo. O volume é arredondado para a etapa de volume de troca e limitado pelo volume mínimo/máximo.
- As ordens de stop protetora rastreiam a posição usando o canal de saída Donchian mais um buffer ATR para evitar a rotatividade excessiva de ordens.

## Regras do Desafio da Prop Firm
- `Use Challenge Rules` permite verificações de desafio.
- A negociação é interrompida quando `Pass Criteria` patrimônio é alcançado. Todas as ordens são canceladas e a posição fechada.
- Rebaixamentos diários maiores que `Daily Loss Limit` acionam uma liquidação completa e desativam novos pedidos pelo restante da sessão. O patrimônio de referência é zerado no início de cada dia de negociação.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Entry Period` | Lookback do canal de breakout Donchian. |
| `Entry Shift` | Número de velas finalizadas ignoradas ao usar o canal de breakout. |
| `Exit Period` | Lookback do canal Donchian final. |
| `Exit Shift` | Número de velas concluídas ignoradas para trailing stops. |
| `Risk Per Trade %` | Porcentagem do patrimônio do portfólio em relação ao risco em cada entrada. |
| `ATR Period` | Lookback para filtro ATR usado ao mover paradas. |
| `Use Challenge Rules` | Permite condições de desafio firmes. |
| `Pass Criteria` | Nível de patrimônio que interrompe negociações adicionais. |
| `Daily Loss Limit` | Rebaixamento diário permitido antes da interrupção da negociação. |
| `Candle Type` | Assinatura de vela usada para cálculos. |

## Notas
- A estratégia requer uma conexão de portfólio para calcular tamanhos de posição baseados em risco e métricas de desafio.
- Os pedidos são cancelados e reenviados em cada vela concluída para manter os preços de gatilho alinhados com os níveis Donchian mais recentes.
- Os parâmetros padrão reproduzem o comportamento do consultor especialista MetaTrader original.
