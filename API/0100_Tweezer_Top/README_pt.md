# Tweezer Top Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Tweezer Top espelha a versão de fundo, mas aparece após uma subida. Duas velas compartilham quase a mesma máxima, mostrando que os compradores não conseguiram ultrapassar certo nível.

Os testes indicam um retorno anual médio de aproximadamente 187%. Funciona melhor no mercado de ações.

A estratégia abre uma posição vendida assim que a segunda vela confirma o teto, esperando uma retração à medida que o momentum de alta perde força.

Um stop ajustado acima das máximas gêmeas mantém o risco sob controle, e a operação encerra se o preço subir novamente acima dessa resistência.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
