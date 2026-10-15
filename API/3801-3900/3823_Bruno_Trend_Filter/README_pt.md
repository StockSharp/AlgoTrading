# Estratégia de Tendências de Bruno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Bruno Trend Strategy é uma versão StockSharp do MetaTrader consultor especialista "Bruno_v1". A estratégia é negociada em velas de 30 minutos e se concentra em sinais de alta sincronizados de vários indicadores clássicos de acompanhamento de tendências e de impulso. Apenas posições longas são abertas, imitando o especialista original que se concentrou em rompimentos de alta confirmados pelo alinhamento do indicador.

## Lógica de negociação

1. **Prazo**: velas de 30 minutos.
2. **Indicadores**:
   - Média Móvel Simples (SMA) com comprimento 4 usada como medidor de momentum de curto prazo.
   - Médias Móveis Exponenciais (EMAs) com comprimentos 8 e 21 para definir a direção da tendência primária.
   - Índice Direcional Médio (ADX) com período 13 para garantir força direcional por meio de componentes +DI e -DI.
   - Stochastic Oscilador com parâmetros %K=21, %D=3, slowing=3 para confirmar o impulso enquanto evita níveis de sobrecompra.
   - MACD (13, 34, 8) para histograma e confirmação de linha de sinal.
   - Parabolic SAR (etapa 0,055, máximo 0,21) para verificar a aceleração ascendente e gerenciar saídas.
3. **Regras de inscrição**:
   - EMA(8) deve estar acima de EMA(21).
   - Filtro ADX: +DI maior que -DI e acima de 20.
   - Filtro Stochastic: %K acima de %D, mas ainda abaixo de 80 para ficar fora dos extremos de sobrecompra.
   - Histograma MACD acima de zero e acima da linha de sinal.
   - Parabolic SAR aumentando (atual SAR maior que a leitura anterior).
   - A posição atual deve ser plana ou curta. Qualquer posição curta é fechada antes de entrar na nova negociação longa.
4. **Regras de saída**:
   - Feche a posição longa quando o fechamento da vela anterior cair abaixo do valor Parabolic SAR anterior, replicando o gatilho de saída MetaTrader.

## Gestão de risco

- Tamanho do lote padrão: 0,1 lote.
- Proteção opcional estilo MetaTrader: take-profit de 50 pip e stop-loss de 30 pip, configurado com `StartProtection`. As paradas finais são desabilitadas por padrão para espelhar o script original.

## Notas

- A estratégia ignora a configuração curta não utilizada do código MetaTrader, correspondendo ao comportamento original onde as negociações curtas foram efetivamente desativadas.
- Os valores dos indicadores são processados por meio do API de alto nível do API para evitar buffer manual e permanecer alinhados com as diretrizes do projeto.
