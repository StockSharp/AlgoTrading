# Estratégia do canal comercial ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Trade Channel replica o consultor especialista MetaTrader original que negociou canais de preços com paradas baseadas em ATR. Ele espera que os limites do canal permaneçam inalterados e que a última vela toque ou rejeite esses níveis. Quando a configuração aparece, a estratégia abre uma posição na direção oposta do toque e aplica um trailing stop adaptativo medido em pontos.

A abordagem procura explorar a reversão à média em torno de um canal de preços estável. Ele filtra os sinais para que o canal esteja plano (sem novos máximos ou mínimos) antes de entrar. Os stops de proteção são colocados além do canal usando o Average True Range, e um trailing stop opcional bloqueia os lucros assim que o movimento se desenvolve.

## Detalhes

- **Critérios de entrada**:
  - Curto: a máxima do canal é igual à máxima do canal anterior e a última vela quebra essa máxima ou fecha entre a máxima e o pivô `(high + low + close) / 3`.
  - Longo: o mínimo do canal é igual ao mínimo do canal anterior e a última vela quebra esse mínimo ou fecha entre o mínimo e o pivô.
- **Longo/Curto**: Ambas as direções, mas apenas uma posição por vez.
- **Critérios de saída**:
  - Longo: o preço atinge a máxima do canal enquanto a máxima permanece inalterada.
  - Curto: o preço atinge a mínima do canal enquanto a mínima permanece inalterada.
  - O trailing stop opcional se estreita atrás do mercado quando o lucro excede `TrailingDistance` pontos.
- **Stops**: Stop loss inicial em `channel boundary ± ATR`. O trailing stop o substitui quando ativado.
- **Valores padrão**:
  - `Volume` = 0,1m
  - `ChannelPeriod` = 20
  - `AtrPeriod` = 4
  - `TrailingDistance` = 30
  - `CandleType` = velas de 30 minutos
- **Filtros**:
  - Categoria: Reversão à Média
  - Direção: Ambos
  - Indicadores: Maior, Mais Baixo, Faixa Verdadeira Média
  - Paradas: ATR parada, Trailing
  - Complexidade: Intermediário
  - Prazo: intradiário (30 minutos)
  - Sazonalidade: Não
  - Redes Neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Notas

- `Volume` controla o tamanho do pedido; apenas uma posição pode existir por vez.
- `TrailingDistance` é especificado em pontos (etapas de preço). Defina como zero para desativar o trailing stop.
- A estratégia requer velas históricas para aquecer os indicadores Maior/Mínimo e ATR antes da negociação.
- As ordens stop são automaticamente canceladas quando a posição fecha ou a estratégia é redefinida.
