# Estratégia Eugene de Rompimento Interno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Eugene de Rompimento Interno é uma portagem direta do consultor especialista original do MetaTrader por barabashkakvn. Ela se concentra na ação do preço pura: uma sequência de velas internas seguida de um rompimento de intervalo. Os níveis de confirmação derivados do corpo da vela anterior garantem que o rompimento desenvolva momentum antes que a estratégia tome uma posição.

## Visão geral

A estratégia observa por uma nova máxima ou mínima em relação à vela anterior. As configurações compradas exigem que a vela anterior tenha uma mínima abaixo da máxima da vela antes dela, destacando a compressão antes do rompimento. As configurações vendidas se recusam a operar se a vela anterior for uma barra interna, espelhando as proteções na lógica MQL fonte. As ordens são sempre executadas a mercado com um volume fixo.

## Lógica de mercado

- Enfatiza os rompimentos da máxima/mínima mais recente para captar movimentos direcionais cedo.
- Usa o corpo da vela anterior para calcular dois níveis de retração de um terço (`zigLevelBuy` e `zigLevelSell`). O preço deve tocar esses níveis, ou a sessão deve estar além da hora de ativação configurada, antes que uma entrada seja permitida.
- Previne novas posições quando um rompimento coincide com uma vela interna contra a direção da operação.
- Fecha posições abertas sempre que o sinal de rompimento oposto confirmar, garantindo que a estratégia esteja sempre plana ou alinhada com o último sinal.

## Regras de entrada

### Comprado

1. A máxima da vela atual é maior do que a máxima da vela anterior.
2. A confirmação é recebida quando a mínima atual perfura a retração de um terço do corpo da vela anterior, ou a hora atual está além do parâmetro de hora de ativação.
3. A mínima atual deve permanecer acima da mínima anterior enquanto a mínima anterior fica abaixo da máxima de duas velas atrás.
4. Nenhuma posição existente está aberta.

### Vendido

1. A mínima da vela atual é menor do que a mínima da vela anterior.
2. A confirmação é recebida quando a máxima atual testa a retração superior de um terço do corpo da vela anterior, ou a hora atual está além do parâmetro de hora de ativação.
3. A vela anterior não deve ser uma barra interna.
4. A máxima atual deve estar abaixo da máxima anterior.
5. Nenhuma posição existente está aberta.

## Regras de saída

- Fechar posições compradas quando um rompimento vendido validado se forma (condições 1–3 da lógica de entrada vendida).
- Fechar posições vendidas quando um rompimento comprado validado se forma (condições 1–3 da lógica de entrada comprada).

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Período das velas processadas pela estratégia. | Velas de 1 hora |
| `Volume` | Tamanho da ordem enviada com cada ordem de mercado. | 0.1 |
| `ActivationHour` | Hora do dia após a qual as confirmações são aceitas automaticamente, replicando o filtro `TimeCurrent()` do código MQL. | 8 |

## Notas

- As verificações de confirmação rotuladas "white bird" e "black bird" no script original sempre se avaliam como falsas devido às condições fonte; elas são preservadas para paridade, mas não afetam as decisões de trading.
- Nenhum indicador adicional ou trailing stop é usado—a abordagem é puramente baseada em preço e vira posições a cada rompimento oposto.
