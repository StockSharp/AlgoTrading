# Estratégia de instrumentos Forex Zs1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz a lógica de grade protegida do especialista MetaTrader **Zs1_www_forex-instruments_info**. O algoritmo abre um par de compra/venda simultâneo, monitora a distância que o preço percorre desde o ponto inicial e reage a cinco zonas de negociação distintas. A média da perna sobrevivente do hedge é calculada com multiplicadores de martingale, enquanto a cesta é protegida por uma saída baseada em ações.

## Comportamento central

- Abra um hedge de mercado inicial (uma compra e uma venda) com o volume base configurado.
- Quando uma das pernas se tornar lucrativa, feche-a e mantenha o lado perdedor como ordem âncora.
- Acompanhe o deslocamento de preço usando o parâmetro `Orders Space (pips)`. Quando uma nova zona for alcançada, execute a mesma lógica de ramificação do especialista original:
  - Zona −2: feche a cesta com base no lucro, caso contrário, faça a média em relação ao movimento.
  - Zona −1: adicione uma posição oposta à âncora inicial.
  - Zona 0: adicione uma posição na direção da âncora.
  - Zona +1: feche a cesta com lucro, caso contrário abra o lado oposto.
- Sempre que três ou mais negociações estiverem ativas, saia imediatamente se o lucro flutuante não for negativo.
- Após todas as posições serem fechadas, o ciclo reinicia automaticamente.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Orders Space (pips)` | Distância em pips entre níveis de grade adjacentes. |
| `Zone Offset (pips)` | Buffer extra que deve ser violado antes que uma nova zona seja confirmada. |
| `Initial Volume` | Volume base utilizado para sebe de abertura e para escalonamento de martingale. |

## Notas

- Os multiplicadores martingale seguem a sequência original do túnel (1, 3, 6, 12, ...).
- A validação do volume respeita as restrições mínimas, máximas e de etapas de segurança antes de enviar qualquer pedido.
- Todas as decisões são orientadas pelas melhores atualizações de compra/venda dos dados do Nível 1, correspondendo à lógica baseada em ticks da versão MQL.
