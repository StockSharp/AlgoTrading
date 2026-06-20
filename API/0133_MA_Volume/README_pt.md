# Estratégia MA Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
MA Volume combina um filtro de tendência de média móvel com picos de volume para cronometrar as entradas.
Volume crescente junto com o preço acima da média sinaliza forte acumulação; volume decrescente abaixo da média indica distribuição.

Os testes indicam um retorno anual médio de aproximadamente 136%. Funciona melhor no mercado de ações.

A estratégia opera na direção da média móvel quando o volume se expande, saindo quando o volume seca ou a média reverte.

Um stop percentual protege contra mudanças repentinas na tendência.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Moving Average, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

