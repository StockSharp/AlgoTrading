# Estratégia de Pivô Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista "SimplePivot" do MetaTrader 5. Avalia continuamente a relação entre a abertura da barra atual e o nível de pivô da barra anterior, mantendo sempre uma única posição direcional. Quando o viés muda, a estratégia fecha a posição existente e abre imediatamente uma na direção oposta.

## Visão geral

- **Regime de mercado**: Swing trading sempre no mercado.
- **Instrumentos**: Qualquer instrumento que forneça dados de velas para o período de tempo selecionado.
- **Períodos de tempo**: Configurável pelo parâmetro *Candle Type* (padrão velas de 1 hora).
- **Ordens**: Ordens de mercado dimensionadas pelo parâmetro *Volume*.

## Como funciona

### Cálculo do pivô

1. Aguardar pelo menos uma vela concluída para inicializar o cálculo.
2. Calcular o pivô da vela anterior como a média aritmética de seus preços de máxima e mínima.
3. Reter a máxima e mínima anteriores para que o pivô para a próxima barra possa ser produzido imediatamente quando uma nova vela terminar.

### Decisão direcional

1. O viés padrão é comprado (compra).
2. Se a vela atual abre abaixo da máxima anterior enquanto permanece acima do pivô, o viés muda para vendido (venda).
3. Se a direção desejada não mudou em relação à última negociação executada, a posição existente é preservada e nenhuma nova ordem é enviada.

### Gestão de posição

1. Se a direção desejada difere da negociação atual, a posição em execução é encerrada por uma ordem de mercado oposta.
2. Após encerrar, uma ordem de mercado dimensionada por *Volume* estabelece a nova exposição direcional.
3. O processo se repete a cada vela concluída, garantindo que a estratégia esteja sempre comprada ou vendida.

## Parâmetros

- **Volume**: Tamanho da negociação usado para cada entrada. Também determina o tamanho da ordem de fechamento quando a estratégia muda de direção.
- **Candle Type**: Tipo de dados das velas usadas para cálculos de pivô e entrada. O padrão é um período de 1 hora, mas qualquer período disponível pode ser selecionado.

## Notas adicionais

- A lógica reage em velas completamente concluídas (`CandleStates.Finished`) para evitar sinais repetidos enquanto uma vela ainda está se formando.
- Nenhum stop ou alvo de lucro é definido; as saídas ocorrem apenas quando a regra de pivô solicita uma mudança de direção.
- Como a estratégia está sempre no mercado, controles de risco como monitoramento de drawdown máximo ou filtros de sessão devem ser gerenciados externamente, se necessário.
