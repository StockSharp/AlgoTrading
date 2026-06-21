# Estratégia de Cruzamento SMA e EMA DNSE VN301
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera o índice VN301 usando um cruzamento entre uma EMA de 15 períodos e uma SMA de 60 períodos. Ela sai antes do encerramento da sessão de negociação e aplica um stop percentual simples para limitar as perdas.

Os testes indicam um retorno anual médio de cerca de 20%. Funciona melhor em futuros VN30.

Uma posição comprada é aberta quando a EMA15 cruza acima da SMA60 e o preço está acima da EMA. Uma posição vendida é aberta no cruzamento oposto. As posições são fechadas em sinais inversos, no encerramento da sessão, ou quando o preço vai contra a entrada além do limite de perda configurado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA15 cruza acima da SMA60 e preço >= EMA15 antes do encerramento.
  - **Vendido**: EMA15 cruza abaixo da SMA60 e preço <= EMA15 antes do encerramento.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto, perda máxima ou encerramento da sessão.
- **Stops**: Sim, perda máxima baseada em percentual.
- **Filtros**:
  - Hora de encerramento da sessão.
