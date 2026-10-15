# Estratégia BBStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

BBStrategy é um sistema de breakout de bandas Bollinger convertido do consultor especialista MetaTrader "BBStrategy". A estratégia rastreia dois conjuntos de bandas Bollinger com o mesmo período, mas multiplicadores de desvio diferentes. Quando o preço ultrapassa a banda externa, o algoritmo arma uma negociação, mas a entrada real é adiada até que o preço retorne à banda interna. Este comportamento tenta evitar a compra de rompimentos excessivamente estendidos ou a venda de condições profundamente sobrevendidas, ao mesmo tempo em que captura o movimento de continuação após uma expansão de volatilidade.

## Lógica principal

1. Assine velas e calcule duas Bollinger bandas:
   - **A banda externa** usa um multiplicador de desvio configurável (padrão 3.0).
   - **Banda interna** usa um desvio menor (padrão 2,0).
2. Detecte quando o preço de fechamento termina fora da banda externa:
   - Acima da faixa externa superior arma uma configuração longa.
   - Abaixo dos braços da faixa externa inferior, uma configuração curta.
3. Insira apenas se a próxima vela concluída fechar dentro da faixa interna na direção do rompimento. Enquanto o preço espera para entrar novamente, a estratégia permanece em estado de “espera” para a direção correspondente.
4. Envie uma única ordem de mercado quando as condições estiverem alinhadas e não houver posições abertas ou ordens ativas. As posições opostas existentes são fechadas aumentando o volume da ordem de mercado.
5. As distâncias opcionais de take-profit e stop-loss (expressas em pontos) são convertidas em compensações de preço absoluto e gerenciadas por meio do auxiliar de proteção integrado.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| **Volume do pedido** | Tamanho da negociação para cada posição. |
| **Bollinger Período** | Número de velas usadas para ambos os cálculos de banda Bollinger. |
| **Desvio Interno** | Multiplicador de desvio para a banda interna que valida retrocessos. |
| **Desvio Externo** | Multiplicador de desvio para a banda externa que detecta fugas. |
| **Pontos Stop-Loss** | Distância de parada protetora em pontos (0 desabilita a parada). |
| **Pontos de lucro** | Distância de take-profit em pontos (0 desativa o alvo). |
| **Tipo de vela** | Prazo de vela para cálculos. |

## Notas

- A estratégia negocia uma única posição de cada vez e ignora novos sinais enquanto as ordens estão ativas.
- Para gerenciamento de risco, o auxiliar converte MetaTrader "pontos" em incrementos de preço reais com base no tamanho do tick do instrumento.
- Os desenhos do gráfico incluem velas, bandas Bollinger e as próprias negociações da estratégia para facilitar a depuração visual.
