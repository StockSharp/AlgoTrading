# Estratégia de Rompimento Diário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos da abertura diária. No início de cada novo dia, o preço de abertura é armazenado. Quando o preço se afasta deste nível por um número de pontos definido pelo utilizador, e a barra anterior está dentro de um intervalo de tamanho configurável, a estratégia entra na direção do rompimento.

## Lógica de entrada

- Se a barra anterior for de alta e o preço subir acima da abertura diária em **Break Point** pontos, uma posição comprada é aberta.
- Se a barra anterior for de baixa e o preço cair abaixo da abertura diária em **Break Point** pontos, uma posição vendida é aberta.
- O tamanho da barra anterior deve estar entre **Last Bar Min** e **Last Bar Max** pontos.
- O nível de rompimento deve estar dentro do corpo da barra anterior.

## Gestão de risco

- O **Take Profit** e **Stop Loss** opcionais são medidos em pontos a partir do preço de entrada.
- Um trailing stop pode ser ativado com os parâmetros **Trailing Start**, **Trailing Stop** e **Trailing Step**. Quando o preço se move favoravelmente em *Trailing Start* pontos, o stop é definido em *Trailing Stop* pontos da entrada e então segue em incrementos de *Trailing Step*.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| Candle Type | Período das velas processadas. |
| Break Point | Distância da abertura diária para acionar um trade (pontos). |
| Last Bar Min | Tamanho mínimo da barra anterior (pontos). |
| Last Bar Max | Tamanho máximo da barra anterior (pontos). |
| Trailing Start | Movimento de preço para iniciar o trailing stop (pontos). |
| Trailing Stop | Distância inicial do trailing stop (pontos). |
| Trailing Step | Passo para mover o trailing stop (pontos). |
| Take Profit | Distância de take profit (pontos). |
| Stop Loss | Distância de stop loss (pontos). |

## Notas

A estratégia opera apenas em velas concluídas e utiliza ordens a mercado para entradas e saídas. Armazena variáveis internas para os dados da barra anterior e o nível do trailing stop.
