# Estratégia de Cruzamento Percentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o comportamento do expert original do MetaTrader `Exp_PercentageCrossover`. Ela opera na direção do indicador Percentage Crossover, que traça uma linha de preço trailing que só pode se mover dentro de uma banda percentual fixa em torno do fechamento atual. A inclinação desta linha define o estado do mercado e desencadeia negociações.

## Conceito

1. Em cada vela completada o indicador mantém o valor anterior da linha.
2. Uma atualização de alta ocorre quando o fechamento empurra a linha trailing acima de seu valor anterior em pelo menos `percent` por cento do preço.
3. Uma atualização de baixa ocorre quando o fechamento puxa a linha trailing abaixo de seu valor anterior pelo mesmo percentual.
4. Se o fechamento permanecer dentro da banda, a linha permanece plana e retém sua última cor.

A cor da linha é interpretada da mesma forma que no MetaTrader:

- **Índice de cor 0 (azul/violeta)** – a linha está subindo (contexto de alta).
- **Índice de cor 1 (laranja)** – a linha está caindo (contexto de baixa).

## Regras de negociação

### Entradas compradas
- Habilitadas apenas quando `BuyPosOpen = true`.
- Avalia a barra selecionada por `SignalBar` (1 significa a última barra fechada).
- Abre uma posição comprada quando essa barra muda da cor 1 para a cor 0.

### Entradas vendidas
- Habilitadas apenas quando `SellPosOpen = true`.
- Avalia a mesma barra `SignalBar`.
- Abre uma posição vendida quando a barra muda da cor 0 para a cor 1.

### Gestão de posições
- Se `BuyPosClose = true`, qualquer posição comprada aberta é fechada sempre que a barra atual (após aplicar o deslocamento `SignalBar`) for de cor 1.
- Se `SellPosClose = true`, qualquer posição vendida aberta é fechada sempre que essa barra for de cor 0.
- Quando `UseTimeFilter = true` e o horário atual estiver fora da janela de negociação configurada, a estratégia sai imediatamente da posição ativa e ignora novos sinais até que o mercado entre novamente na janela.
- As ordens são enviadas com `BuyMarket()` e `SellMarket()`. A quantidade real vem da propriedade `Volume` da estratégia.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Percent` | Banda percentual para a linha trailing. Valores mais altos fazem a linha reagir mais lentamente. | `1` |
| `SignalBar` | Qual barra fechada é analisada (1 = última fechada). Deve permanecer positivo. | `1` |
| `BuyPosOpen` / `SellPosOpen` | Habilitar entradas compradas ou vendidas respectivamente. | `true` |
| `BuyPosClose` / `SellPosClose` | Habilitar a lógica de fechamento para posições compradas ou vendidas. | `true` |
| `UseTimeFilter` | Ativar a janela de negociação. | `true` |
| `StartHour` / `StartMinute` | Hora e minuto que abrem a janela de negociação quando o filtro está ativo. | `0` / `0` |
| `EndHour` / `EndMinute` | Hora e minuto que fecham a janela de negociação. | `23` / `59` |
| `CandleType` | Período das velas usadas para o indicador e os sinais. | `4h` |

## Notas

- O filtro de tempo segue rigorosamente o Expert Advisor original. Quando a hora de início é maior que a hora de fim, a lógica cria uma janela noturna, mas ainda exige que os minutos sejam maiores ou iguais a `StartMinute` para que a sessão se torne ativa.
- `SignalBar` é avaliado apenas em velas finalizadas. Defina-o como `1` para espelhar a configuração padrão do MetaTrader.
- A estratégia não impõe níveis de stop-loss ou take-profit. O controle de risco deve ser gerenciado externamente ou ajustando o percentual e a janela de negociação.
