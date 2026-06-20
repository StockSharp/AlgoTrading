# Estratégia de Reversão por Clímax de Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Reversão por Clímax de Volume busca pontos de virada marcados por volume extremamente alto após uma tendência forte.
Esses picos climáticos sugerem esgotamento à medida que os últimos compradores ou vendedores se precipitam antes que o momentum desapareça.

Os testes indicam um retorno anual médio de aproximadamente 82%. Funciona melhor no mercado de ações.

A estratégia entra contra o movimento anterior assim que uma barra de grande volume fecha e o preço começa a recuar.

Um stop percentual apertado protege a posição, e as operações saem se o volume não diminuir ou o preço continuar na direção original.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

